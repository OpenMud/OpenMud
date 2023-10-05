using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Client.Terminal.Components;

namespace OpenMud.Mudpiler.Client.Terminal.Systems;

[With(typeof(TextAnimationComponent))]
public class TerminalAnimatorSystem : AEntitySetSystem<float>
{
    private static readonly float FRAME_LIFETIME = 1.0f / 10f;

    public TerminalAnimatorSystem(World world, bool useBuffer = false) : base(world, useBuffer)
    {
    }

    protected override void Update(float state, in Entity entity)
    {
        var animation = entity.Get<TextAnimationComponent>();
        animation.TimeSinceToggle += state;

        var framesAdvance = (int)(animation.TimeSinceToggle / FRAME_LIFETIME);
        animation.TimeSinceToggle %= FRAME_LIFETIME;

        var oldFrameIndex = animation.FrameIndex;
        animation.FrameIndex = (animation.FrameIndex + framesAdvance) % animation.Frames.Length;

        if (oldFrameIndex != animation.FrameIndex || !entity.Has<CharacterGraphicComponent>())
        {
            var frame = animation.Frames[animation.FrameIndex];
            entity.Set(new CharacterGraphicComponent(frame.Colour, frame.BackgroundColour, frame.Symbol));
        }
    }
}