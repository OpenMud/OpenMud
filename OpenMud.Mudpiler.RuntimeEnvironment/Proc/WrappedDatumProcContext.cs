using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public class WrappedDatumProcContext
{
    private readonly DatumProcExecutionContext ctx;
    private readonly Func<EnvObjectReference, object> wrapper;

    public WrappedDatumProcContext(DatumProcExecutionContext ctx, Func<EnvObjectReference, object> wrapper)
    {
        this.ctx = ctx;
        this.wrapper = wrapper;
    }

    public object Result => wrapper(ctx.Result);

    public DatumProcExecutionState State => ctx.State;

    public object CompleteOrException()
    {
        return wrapper(ctx.CompleteOrException());
    }

    public bool UnmanagedContinue()
    {
        ctx.UnmanagedContinue();

        return ctx.State == DatumProcExecutionState.Completed;
    }

    public bool Continue()
    {
        ctx.Continue();
        return ctx.State == DatumProcExecutionState.Completed;
    }
}