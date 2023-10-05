using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(PlayerCanImpersonateComponent))]
public class CommandDiscoverySystem : AEntitySetSystem<float>
{
    private readonly EntityVisibilitySolver visibilitySolver;

    public CommandDiscoverySystem(World world, EntityVisibilitySolver visibilitySolver, bool useBuffer = false) : base(
        world, useBuffer)
    {
        this.visibilitySolver = visibilitySolver;
    }

    private IEnumerable<CommandDetails> DiscoverVisibleCommands(in Entity entity)
    {
        List<CommandDetails> visibleCommands = new();

        var visible = visibilitySolver.ComputeVisible(World, entity);

        bool isVisible(in IdentifierComponent p)
        {
            return visible.ContainsKey(p.Name);
        }

        var interactable = World
            .GetEntities()
            .With<VerbDetailsComponent>()
            .With<IdentifierComponent>(isVisible)
            .AsEnumerable();

        foreach (var e in interactable)
            if (e != entity)
                visibleCommands.AddRange(
                    VerbConstraintSolver.DiscoverExternalInteractionCommands(World, visibilitySolver, entity, e, 0));

        return visibleCommands;
    }

    protected override void Update(float state, in Entity entity)
    {
        List<CommandDetails> availableCommands = new();

        if (entity.Has<VerbDetailsComponent>())
            availableCommands.AddRange(VerbConstraintSolver.DiscoverSelfCommands(World, visibilitySolver, entity, 2));

        if (entity.Has<AtomicContentsComponent>())
        {
            var contents = entity.Get<AtomicContentsComponent>();

            bool isContents(in IdentifierComponent p)
            {
                return contents.Contents.Contains(p.Name);
            }

            if (contents.Contents.Any())
            {
                var interactable = World
                    .GetEntities()
                    .With<VerbDetailsComponent>()
                    .With<IdentifierComponent>(isContents)
                    .AsEnumerable();

                foreach (var e in interactable)
                    if (e != entity)
                        availableCommands.AddRange(
                            VerbConstraintSolver.DiscoverExternalInteractionCommands(World, visibilitySolver, entity, e,
                                1));
            }
        }

        availableCommands.AddRange(DiscoverVisibleCommands(entity));

        var newCommandsComp = new ActionableCommandsComponent(availableCommands.ToArray());

        if (entity.Has<ActionableCommandsComponent>() &&
            entity.Get<ActionableCommandsComponent>().Equals(newCommandsComp))
            return;

        entity.Set(new ActionableCommandsComponent(availableCommands.ToArray()));
    }
}