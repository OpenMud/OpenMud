using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(ExecuteCommandComponent))]
public class CommandDispatcherService : AEntitySetSystem<float>
{
    public CommandDispatcherService(World world, bool useBuffer = false) : base(world, useBuffer)
    {
    }

    protected override void Update(float state, in Entity entity)
    {
        var subject = entity.Get<ExecuteCommandComponent>();

        bool isNameMatch(in IdentifierComponent p)
        {
            return p.Name.Equals(subject.Source);
        }

        var srcEntity = World.GetEntities().With<ActionableCommandsComponent>().With<IdentifierComponent>(isNameMatch)
            .AsEnumerable().SingleOrDefault();

        entity.Remove<ExecuteCommandComponent>();

        if (!srcEntity.IsAlive)
        {
            World.Publish(new CommandRejectionMessage(subject.Source, "Source has no actionable commands."));
        }
        else
        {
            var actionable = srcEntity.Get<ActionableCommandsComponent>();
            var hasTarget = subject.Operands.Length > 0;
            var cmdMatch = actionable
                .Commands
                .OrderByDescending(
                    x => x.Precedent
                )
                .Where(x => x.Verb.ToLower() == subject.Verb.ToLower())
                .Where(x =>
                    x.Target == null ||
                    (hasTarget && x.TargetName != null && x.TargetName.ToLower() == subject.Operands[0].ToLower())
                )
                .Where(x => subject.Target == null || subject.Target == x.Target)
                .FirstOrDefault();

            if (cmdMatch.IsNull)
            {
                World.Publish(new CommandRejectionMessage(subject.Source,
                    "Unable to match command with known actionable commands."));
                return;
            }

            var operands = cmdMatch.TargetName == null ? subject.Operands : subject.Operands.Skip(1).ToArray();

            entity.Set(new ExecuteVerbComponent(subject.Source, cmdMatch.Target ?? subject.Source,
                subject.Verb.ToLower(), operands));
        }
    }
}