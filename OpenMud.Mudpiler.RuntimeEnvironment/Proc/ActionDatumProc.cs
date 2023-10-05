using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public delegate EnvObjectReference CallerAwareAtomicDatumProc(ProcArgumentList args, Datum self,
    DatumProcExecutionContext caller);

public delegate EnvObjectReference AtomicDatumProc(ProcArgumentList args, Datum self);

public delegate EnvObjectReference SimpleAtomicDatumProc(ProcArgumentList args);

public class ActionDatumProcContext : DatumProcExecutionContext
{
    private readonly CallerAwareAtomicDatumProc handler;

    private dynamic result = 0;

    private readonly bool singleFire;

    public ActionDatumProcContext(CallerAwareAtomicDatumProc handler, bool singleFire = false)
    {
        this.handler = handler;
        this.singleFire = singleFire;
    }

    public ActionDatumProcContext(AtomicDatumProc handler, bool singleFire = false) : this((a, b, _) => handler(a, b),
        singleFire)
    {
    }

    public ActionDatumProcContext(ActionAtomicDatumProc h, bool singleFire = false) : this((args, _) => h(), singleFire)
    {
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
        if (State == DatumProcExecutionState.Completed)
            return;

        //If exception is thrown etc, we proactively set the state to completed
        if (singleFire)
            State = DatumProcExecutionState.Completed;

        result = handler(ActiveArguments, self, Caller);
        State = DatumProcExecutionState.Completed;
    }

    protected override DatumProcExecutionContext GenerateClone(DatumProcExecutionContext newCaller)
    {
        throw new NotImplementedException();
    }
}

public class ActionDatumProc : DatumProc
{
    private readonly IDmlProcAttribute[] attributes;
    private readonly AtomicDatumProc handler;
    private readonly bool singleFire;

    public ActionDatumProc(string name, IDmlProcAttribute[]? attr, AtomicDatumProc handler, bool singleFire = false)
    {
        attributes = attr ?? new IDmlProcAttribute[0];
        this.handler = handler;
        this.singleFire = singleFire;
        Name = name;
    }

    public ActionDatumProc(string name, AtomicDatumProc handler) : this(name, null, handler)
    {
    }


    public ActionDatumProc(string name, IDmlProcAttribute[]? attr, SimpleAtomicDatumProc handler,
        bool singleFire = false) : this(name, attr, (args, _) => handler(args), singleFire)
    {
    }

    public ActionDatumProc(string name, SimpleAtomicDatumProc handler, bool singleFire = false) : this(name, null,
        (args, _) => handler(args), singleFire)
    {
    }

    public override string Name { get; }

    public override IDmlProcAttribute[] Attributes()
    {
        return attributes;
    }

    public override DatumProcExecutionContext Create()
    {
        return new ActionDatumProcContext(handler, singleFire);
    }
}