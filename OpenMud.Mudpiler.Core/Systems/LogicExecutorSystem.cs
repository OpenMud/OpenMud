using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Utils;

namespace OpenMud.Mudpiler.Core.Systems;

public class LogicExecutionException : Exception
{
    public LogicExecutionException(string message) : base(message)
    {
    }
}

[With(typeof(ExecuteLogicComponent))]
public class LogicExecutorSystem : AEntitySetSystem<float>
{
    private readonly LogicDirectory logicDirectory;

    public LogicExecutorSystem(World ecsWorld, LogicDirectory logicDirectory, bool useBuffer = false) : base(ecsWorld,
        useBuffer)
    {
        this.logicDirectory = logicDirectory;
    }

    private void ProcessExecute(in Entity entity)
    {
        var exec = entity.Get<ExecuteLogicComponent>();

        bool MatchesDestination(in IdentifierComponent i)
        {
            return i.Name == exec.DestinationName;
        }

        bool MatchesSource(in IdentifierComponent i)
        {
            return i.Name == exec.SourceName;
        }

        var target = World.GetEntities().With<IdentifierComponent>(MatchesDestination).AsEnumerable().FirstOrDefault();

        if (!target.IsAlive || !target.Has<LogicIdentifierComponent>())
            throw new LogicExecutionException("Target doesn't exist.");

        var targetDatum = logicDirectory[target.Get<LogicIdentifierComponent>().LogicInstanceId];

        if (exec.SourceName == null)
        {
            targetDatum.ExecProc(exec.MethodName, exec.Arguments);
        }
        else
        {
            var source = World.GetEntities().With<IdentifierComponent>(MatchesSource).AsEnumerable().FirstOrDefault();

            if (!source.IsAlive || !source.Has<LogicIdentifierComponent>())
                throw new LogicExecutionException("Source does not exist.");

            var sourceDatum = logicDirectory[source.Get<LogicIdentifierComponent>().LogicInstanceId];

            sourceDatum.ExecProcOn(targetDatum, exec.MethodName, exec.Arguments);
        }
    }

    protected override void Update(float state, in Entity entity)
    {
        try
        {
            ProcessExecute(entity);
        }
        catch (LogicExecutionException ex)
        {
            World.Publish(new LogicExecutionFailureMessage { Reason = ex.Message });
        }
        finally
        {
            entity.Dispose();
        }
    }
}