using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class SimpleLogicInteractionProcessor : ITransactionProcessor
{
    private readonly EnvObjectReference baseInstance;
    private readonly EnvObjectReference globalInstance;
    private readonly ExecuteTransaction setupExecutor;

    public SimpleLogicInteractionProcessor(EnvObjectReference baseInstance, EnvObjectReference globalInstance,
        ExecuteTransaction executor)
    {
        setupExecutor = executor;
        this.globalInstance = globalInstance;
        this.baseInstance = baseInstance;
    }

    public DatumProcExecutionContext Invoke(DatumProcExecutionContext? caller, string name, ProcArgumentList args)
    {
        var host = ResolveMethod(name, true);

        var r = setupExecutor.Invoke(caller, null, host, args, name);

        return r;
    }

    public DatumProcExecutionContext InvokeOn(DatumProcExecutionContext? caller, EnvObjectReference subject,
        string name, ProcArgumentList args)
    {
        var host = ResolveMethod(name, subject: subject, searchGlobal: false);

        var r = setupExecutor.Invoke(caller, baseInstance, host, args, name);

        return r;
    }

    public DatumProcExecutionContext InvokePrec(DatumProcExecutionContext? caller, string name, long prec,
        ProcArgumentList args)
    {
        var host = ResolveMethod(name, true);

        var r = setupExecutor.Invoke(caller, null, host, args, name, prec);

        return r;
    }

    public bool ProcExists(string name)
    {
        var host = FindMethod(name, false);

        return host != null;
    }

    public DatumProcExecutionContext TryInvokePrec(DatumProcExecutionContext? caller, string name, long prec, ProcArgumentList args)
    {
        var host = ResolveMethod(name, true);

        var r = setupExecutor.Invoke(caller, null, host, args, name, prec, true);

        return r;
    }

    private EnvObjectReference FindMethod(string name, bool searchGlobal = true, EnvObjectReference? subject = null)
    {
        var subjectInstance = subject == null ? baseInstance : subject;

        if (subjectInstance.Get<Datum>().HasProc(name))
            return subjectInstance;


        if (searchGlobal && globalInstance != null && globalInstance.Get<Datum>().HasProc(name))
            return globalInstance;

        return null;
    }

    private EnvObjectReference ResolveMethod(string name, bool searchGlobal = false, EnvObjectReference? subject = null, bool returnNullOnMissing = false)
    {
        var host = FindMethod(name, searchGlobal, subject);

        if (host == null && !returnNullOnMissing)
            throw new Exception("Method doesn't exist: " + name);

        return host;
    }
}