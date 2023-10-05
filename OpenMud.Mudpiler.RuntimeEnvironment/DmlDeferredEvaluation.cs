using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class DmlDeferredEvaluation
{
    private readonly DatumProcExecutionContext hostContext;
    private readonly Func<EnvObjectReference[], object> run;
    private readonly DmlDeferredEvaluation[] dependencies;
    private bool didExecuteInterim;
    private bool didImmediateRan;
    private object? executeIterim;
    private EnvObjectReference immediateResult = VarEnvObjectReference.NULL;

    public DmlDeferredEvaluation(DmlDeferredEvaluation src, DatumProcExecutionContext newContext)
    {
        run = src.run;
        dependencies = src.dependencies.Select(d => d.Clone(newContext)).ToArray();
        hostContext = src.hostContext;
        didExecuteInterim = src.didExecuteInterim;
        executeIterim = src.executeIterim;

        if (executeIterim is DatumProcExecutionContext ctx)
            executeIterim = ctx.Clone(newContext);
    }

    public DmlDeferredEvaluation(DatumProcExecutionContext hostContext, Func<EnvObjectReference[], object> run)
    {
        dependencies = new DmlDeferredEvaluation[0];
        this.run = run;
        this.hostContext = hostContext;
    }

    public DmlDeferredEvaluation(DatumProcExecutionContext hostContext, DmlDeferredEvaluation[] dependencies,
        Func<EnvObjectReference[], object> run)
    {
        this.dependencies = dependencies;
        this.run = run;
        this.hostContext = hostContext;
    }

    public dynamic ExecuteDynamic()
    {
        return Execute();
    }

    public EnvObjectReference Execute()
    {
        if (!didExecuteInterim)
        {
            executeIterim = run(dependencies.Select(x => x.Execute()).ToArray());

            if (executeIterim is DatumProcExecutionContext setupCtx)
                setupCtx.SetupContext(setupCtx.Caller ?? hostContext.Caller, setupCtx.usr ?? hostContext.usr,
                    setupCtx.self ?? hostContext.self, setupCtx.ctx ?? hostContext.ctx);

            didExecuteInterim = true;
        }

        if (executeIterim is DatumProcExecutionContext ctx)
            return ExecuteGeneratedProcContext(ctx);
        if (executeIterim is EnvObjectReference imm)
            return ExecuteGeneratedImmediate(imm);
        throw new Exception("Did not generate an immediate or a datum proc exec ctx.");
    }

    private EnvObjectReference ExecuteGeneratedProcContext(DatumProcExecutionContext ctx)
    {
        if (ctx.State != DatumProcExecutionState.Completed)
            ctx.UnmanagedContinue();

        if (ctx.State != DatumProcExecutionState.Completed)
            throw new Exception("Did not complete, but also did not raise a defer execution exception.");

        return ctx.Result;
    }

    private EnvObjectReference ExecuteGeneratedImmediate(EnvObjectReference immediate)
    {
        if (didImmediateRan)
            return immediateResult;

        immediateResult = immediate;

        didImmediateRan = true;

        return immediateResult;
    }

    public DmlDeferredEvaluation Clone(DatumProcExecutionContext newContext)
    {
        return new DmlDeferredEvaluation(this, newContext);
    }
}