using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public delegate EnvObjectReference ActionAtomicDatumProc();

public class PreparedDatumProcContext : DatumProcExecutionContext
{
    private readonly ActionAtomicDatumProc handler;

    private dynamic result = 0;

    public PreparedDatumProcContext(ActionAtomicDatumProc handler)
    {
        this.handler = handler;
    }

    public override DatumProcExecutionContext Caller { get; set; }
    public override ProcArgumentList ActiveArguments { get; set; }
    public override DatumExecutionContext ctx { get; set; }
    public override long precedence { get; set; }
    public override dynamic self { get; set; }
    public override dynamic usr { get; set; }
    public override EnvObjectReference Result => result;

    protected override void ContinueHandler()
    {
        result = handler();
        State = DatumProcExecutionState.Completed;
    }

    protected override DatumProcExecutionContext GenerateClone(DatumProcExecutionContext newCaller)
    {
        return new PreparedDatumProcContext(handler);
    }
}