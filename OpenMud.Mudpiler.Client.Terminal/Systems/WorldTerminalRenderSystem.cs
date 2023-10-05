using System.Collections.Immutable;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Client.Terminal.Components;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Systems;
using OpenTK;
using SadConsole;
using SadConsole.UI.Controls;

namespace OpenMud.Mudpiler.Client.Terminal.Systems;

[With(typeof(CharacterGraphicComponent), typeof(PositionComponent))]
public class WorldTerminalRenderSystem : AEntitySetSystem<float>
{
    private readonly EntityVisibilitySolver visibilitySolver;

    private IImmutableSet<string>? ConstrainedVisiblity;
    private readonly DrawingArea target;
    private readonly Dictionary<Tuple<int, int>, int> zBackgroundBuffer = new();
    private readonly Dictionary<Tuple<int, int>, int> zGlyphBuffer = new();

    public WorldTerminalRenderSystem(EntityVisibilitySolver visibilitySolver, World world, DrawingArea target,
        bool useBuffer = false) : base(world, useBuffer)
    {
        this.target = target;
        this.visibilitySolver = visibilitySolver;
    }

    private Vector2d LookAt { get; set; } = Vector2d.Zero;

    protected override void PreUpdate(float state)
    {
        zBackgroundBuffer.Clear();
        zGlyphBuffer.Clear();
        target.Surface.Clear();
        ConstrainedVisiblity = null;

        var player = World.GetEntities().With<PlayerImpersonatingComponent>().AsEnumerable().Take(1).FirstOrDefault();
        var camera = World.GetEntities().With<PositionComponent>().With<CameraComponent>().AsEnumerable()
            .FirstOrDefault();

        if (!player.IsAlive || !camera.IsAlive)
            return;

        if (!player.Has<EntityVisibilityComponent>())
            return;

        ConstrainedVisiblity = player.Get<EntityVisibilityComponent>().VisibleEntities;

        var pos = camera.Get<PositionComponent>();
        LookAt = new Vector2d(pos.x, pos.y);

        ConstrainedVisiblity = visibilitySolver.ComputeVisible(World, in camera).Keys.ToImmutableHashSet();
    }

    protected override void Update(float state, in Entity entity)
    {
        if (ConstrainedVisiblity != null && entity.Has<IdentifierComponent>())
            if (!ConstrainedVisiblity.Contains(entity.Get<IdentifierComponent>().Name))
                return;

        var location = entity.Get<PositionComponent>();
        var chr = entity.Get<CharacterGraphicComponent>();

        var minX = LookAt.X - target.Width / 2;
        var maxX = LookAt.X + target.Width / 2;

        var minY = LookAt.Y - target.Height / 2;
        var maxY = LookAt.Y + target.Height / 2;

        if (location.x < minX || location.x > maxX || location.y > maxY || location.y < minY)
            return;

        var offsetX = (int)(-LookAt.X + target.Width / 2);
        var offsetY = (int)(-LookAt.Y + target.Height / 2);

        var renderLocation = Tuple.Create(location.x + offsetX, location.y + offsetY);

        var glyphRelative = 0;
        var backgroundRelative = chr.BackgroundColour == null ? -1 : 0;

        if (zGlyphBuffer.TryGetValue(renderLocation, out var existingZ))
            glyphRelative = location.z - existingZ;

        if (backgroundRelative >= 0 && zBackgroundBuffer.TryGetValue(renderLocation, out var exisitngZBgr))
            backgroundRelative = location.z - exisitngZBgr;

        if (backgroundRelative >= 0 && chr.BackgroundColour != null)
        {
            target.Surface.SetBackground(renderLocation.Item1, renderLocation.Item2, chr.BackgroundColour.Value);
            zBackgroundBuffer[renderLocation] = location.z;
        }

        if (glyphRelative >= 0)
        {
            target.Surface.SetForeground(renderLocation.Item1, renderLocation.Item2, chr.Colour);
            target.Surface.SetGlyph(renderLocation.Item1, renderLocation.Item2, chr.Text);
            zGlyphBuffer[renderLocation] = location.z;
        }
    }

    protected override void PostUpdate(float state)
    {
        target.IsDirty = true;
    }
}