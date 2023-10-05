using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class DatumHandle
{
    private readonly DestoryCallback destroyAllReferences;
    private readonly ITransactionProcessor execHandler;
    protected Func<DatumHandle, object> lookup;
    private readonly IDmlTaskScheduler scheduler;
    protected Func<object, EnvObjectReference> unwrap;
    protected Func<EnvObjectReference, object> wrap;

    public DatumHandle(MudEnvironment environment, IDmlTaskScheduler scheduler, Func<DatumHandle, object> lookup,
        Func<EnvObjectReference, object> wrap, Func<object, EnvObjectReference> unwrap,
        ITransactionProcessor execHandler, DestoryCallback destroyAllReferences)
    {
        Environment = environment;
        this.destroyAllReferences = destroyAllReferences;
        this.lookup = lookup;
        this.wrap = wrap;
        this.execHandler = execHandler;
        this.unwrap = unwrap;
        this.scheduler = scheduler;
    }

    protected EnvObjectReference self => (EnvObjectReference)lookup(this);

    public MudEnvironment Environment { get; private set; }

    public Type Type => self.Type;

    public dynamic this[string name]
    {
        get => wrap(VarEnvObjectReference.CreateImmutable(self.GetField<object>(name)));

        set
        {
            var refType = unwrap(value);
            self.SetField(name, refType);
        }
    }

    public bool HasProc(string procedure)
    {
        return execHandler.ProcExists(procedure);
    }

    public WrappedDatumProcContext ExecProc(string procedure, object[] arguments, bool start = true)
    {
        return ExecProc(procedure, new WrappedProcArgumentList(arguments), start);
    }

    public WrappedDatumProcContext ScheduleExecProc(string procedure, WrappedProcArgumentList? arguments = null,
        int deferMs = 0)
    {
        if (arguments == null)
            arguments = new WrappedProcArgumentList();

        var r = execHandler.Invoke(null, procedure, arguments.Unwrap(unwrap));

        scheduler.DeferExecution(r, deferMs);

        return new WrappedDatumProcContext(r, wrap);
    }

    public WrappedDatumProcContext ExecProc(string procedure, WrappedProcArgumentList? arguments, bool start = true)
    {
        if (arguments == null)
            arguments = new WrappedProcArgumentList();

        var r = execHandler.Invoke(null, procedure, arguments.Unwrap(unwrap));

        if (start)
            r.Continue();

        return new WrappedDatumProcContext(r, wrap);
    }

    public WrappedDatumProcContext ExecProc(string procedure, params object[] arguments)
    {
        return ExecProc(procedure, new WrappedProcArgumentList(arguments), true);
    }

    public WrappedDatumProcContext ExecProcOn(EntityHandle target, string procedure, WrappedProcArgumentList? arguments,
        bool start = true)
    {
        if (arguments == null)
            arguments = new WrappedProcArgumentList();

        var atomicTarget = unwrap(target);

        if (!typeof(Atom).IsAssignableFrom(atomicTarget.Type))
            throw new Exception("Target is not a valid atomic.");

        var r = execHandler.InvokeOn(null, atomicTarget, procedure, arguments.Unwrap(unwrap));

        if (start)
            r.Continue();

        return new WrappedDatumProcContext(r, wrap);
    }


    public WrappedDatumProcContext ExecProcOn(EntityHandle target, string procedure, params object[] arguments)
    {
        return ExecProcOn(target, procedure, new WrappedProcArgumentList(arguments), true);
    }

    public T Unwrap<T>()
    {
        return (T)unwrap(self).Target;
    }

    public void Destroy()
    {
        destroyAllReferences();
    }
}