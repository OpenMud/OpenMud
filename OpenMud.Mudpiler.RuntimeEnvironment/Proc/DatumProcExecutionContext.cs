using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public enum DatumProcExecutionState
{
    Running,
    Completed
}

public abstract class DatumProcExecutionContext
{
    public abstract DatumProcExecutionContext Caller { get; set; }
    public abstract ProcArgumentList ActiveArguments { get; set; }
    public abstract long precedence { get; set; }
    public abstract dynamic self { get; set; }
    public abstract DatumExecutionContext ctx { get; set; }

    public abstract dynamic usr { get; set; }

    public abstract EnvObjectReference Result { get; }
    public EnvObjectReference ImplicitReturn { get; private set; }

    public DatumProcExecutionState State { get; protected set; }

    protected abstract void ContinueHandler();

    public void UnmanagedContinue()
    {
        ctx.ClearExecutionDeferal(this);
        ContinueHandler();
    }

    public void SetImplicitReturn(EnvObjectReference r)
    {
        ImplicitReturn = r;
    }

    public DatumProcExecutionContext SetupContext(DatumProcExecutionContext? caller, Datum? usr, Datum self,
        DatumExecutionContext ctx)
    {
        Caller = caller;
        this.usr = usr;

        State = DatumProcExecutionState.Running;

        this.self = self;
        this.ctx = ctx;

        return this;
    }

    public EnvObjectReference CompleteOrException()
    {
        if (State != DatumProcExecutionState.Completed)
            ContinueHandler();

        if (State != DatumProcExecutionState.Completed)
            throw new Exception("Procedure did not complete.");

        return Result;
    }

    public void Continue()
    {
        try
        {
            UnmanagedContinue();
        }
        catch (DeferExecutionException e)
        {
            ctx.DeferExecution(this, e.LengthMilliseconds);
        }
    }

    protected DmlDeferredEvaluation CreateDeferred(Func<EnvObjectReference[], object> run)
    {
        return new DmlDeferredEvaluation(this, run);
    }

    protected DmlDeferredEvaluation CreateDeferred(DmlDeferredEvaluation[] dependencies,
        Func<EnvObjectReference[], object> run)
    {
        return new DmlDeferredEvaluation(this, dependencies, run);
    }

    public DatumProcExecutionContext GetExecutingContext()
    {
        return this;
    }

    protected abstract DatumProcExecutionContext GenerateClone(DatumProcExecutionContext newCallerCtx);

    public DatumProcExecutionContext Clone(DatumProcExecutionContext newCallerContext)
    {
        var duplicate = GenerateClone(newCallerContext);

        duplicate.SetupContext(newCallerContext, usr, self, ctx);
        duplicate.precedence = precedence;
        duplicate.ActiveArguments = ActiveArguments;

        return duplicate;
    }
}