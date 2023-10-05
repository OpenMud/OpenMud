using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public delegate DatumProcExecutionContext ChainedAtomicDatumProc(EnvObjectReference inner);

public class PreparedChainDatumProcContext : DatumProcExecutionContext
{
    private readonly ChainedAtomicDatumProc handler;
    private readonly DatumProcExecutionContext inner;
    private DatumProcExecutionContext? outer;


    private dynamic result = 0;

    public PreparedChainDatumProcContext(DatumProcExecutionContext inner, ChainedAtomicDatumProc handler)
    {
        this.handler = handler;
        this.inner = inner;
        ctx = inner.ctx;
        self = inner.self;
    }

    public override DatumProcExecutionContext Caller
    {
        get => inner.Caller;
        set => inner.Caller = value;
    }

    public override DatumExecutionContext ctx
    {
        get => inner.ctx;
        set => inner.ctx = value;
    }

    public override long precedence
    {
        get => inner.precedence;
        set => inner.precedence = value;
    }

    public override dynamic self
    {
        get => inner.self;
        set => inner.self = value;
    }

    public override dynamic usr
    {
        get => inner.usr;
        set => inner.usr = value;
    }

    public override EnvObjectReference Result => result;

    public override ProcArgumentList ActiveArguments
    {
        get => inner.ActiveArguments;
        set => inner.ActiveArguments = value;
    }

    protected override void ContinueHandler()
    {
        if (State == DatumProcExecutionState.Completed)
            return;

        var interimResult = inner.Result;

        if (inner.State != DatumProcExecutionState.Completed)
        {
            inner.UnmanagedContinue();
            interimResult = inner.Result;
        }

        if (inner.State == DatumProcExecutionState.Completed)
        {
            if (outer == null)
            {
                outer = handler(interimResult);
                outer.SetupContext(outer.Caller ?? Caller, outer.usr ?? usr, outer.self ?? self, outer.ctx ?? ctx);
            }

            if (outer.State != DatumProcExecutionState.Completed)
                outer.UnmanagedContinue();

            result = outer.Result;
            State = outer.State;
        }
    }

    protected override DatumProcExecutionContext GenerateClone(DatumProcExecutionContext newCallerCtx)
    {
        return new PreparedChainDatumProcContext(inner.Clone(newCallerCtx), handler);
    }
}