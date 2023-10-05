using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;

namespace OpenMud.Mudpiler.Core.Systems;

public class GameFlowSystem : AEntitySetSystem<float>
{
    private readonly LogicDirectory logicLookup;

    public GameFlowSystem(World world, LogicDirectory logicLookup, bool useBuffer = false) : base(world, useBuffer)
    {
        this.logicLookup = logicLookup;
        world.SubscribeComponentAdded<PlayerImpersonatingComponent>(PlayerImpersonating);
        world.SubscribeComponentAdded<LogicCreatedComponent>(LogicCreated);
    }

    private void LogicCreated(in Entity entity, in LogicCreatedComponent value)
    {
        if (entity.Has<PlayerImpersonatingComponent>())
            logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId].ScheduleExecProc("Login");
    }

    private void PlayerImpersonating(in Entity entity, in PlayerImpersonatingComponent value)
    {
        if (!entity.Has<LogicIdentifierComponent>())
            return;

        logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId].ScheduleExecProc("Login");
    }
}