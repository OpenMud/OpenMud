using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class SpawnDatumProcContext : DmlDatumProcExecutionContext
{
    private DatumProcExecutionContext? blockCaller;

    public SpawnDatumProcContext(DatumProcExecutionContext? blockCaller)
    {
        this.blockCaller = blockCaller;
    }

    public SpawnDatumProcContext()
    {
    }

    protected override DmlDatumProcExecutionContext DmlGenerateClone()
    {
        return new SpawnDatumProcContext(blockCaller);
    }

    protected override object DoContinue()
    {
        if (Caller == null)
            throw new Exception("Spawn must be called from a DML procedure.");

        var delay = 0;
        if (ActiveArguments.MaxPositionalArgument >= 1)
            delay = ActiveArguments[0].Get<int>();

        if (blockCaller == null)
        {
            blockCaller = Caller;
            var proceedCaller = Caller.Clone(blockCaller.Caller);

            ctx.DeferExecution(proceedCaller, delay * 100);

            return VarEnvObjectReference.CreateImmutable(false);
        }

        if (blockCaller == Caller)
            return VarEnvObjectReference.CreateImmutable(false);
        return VarEnvObjectReference.CreateImmutable(true);
    }

    protected override void SetupPositionalArguments(object[] args)
    {
    }
}

internal class SpawnDatumProc : DatumProc
{
    public override string Name => "spawn";

    public override IDmlProcAttribute[] Attributes()
    {
        return new IDmlProcAttribute[0];
    }

    public override DatumProcExecutionContext Create()
    {
        return new SpawnDatumProcContext();
    }
}

internal class GlobalTasks : IRuntimeTypeBuilder
{
    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0, new ActionDatumProc("sleep", args => sleep(args[0]), true));
        procedureCollection.Register(0, new SpawnDatumProc());
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.HasImmediateBaseTypeDatum(target, DmlPrimitiveBaseType.Global);
    }

    public EnvObjectReference sleep(EnvObjectReference delay)
    {
        var sleepTime = delay.Get<int>();

        throw new DeferExecutionException(sleepTime * 100);
    }
}