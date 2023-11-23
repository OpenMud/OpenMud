using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

public class Datum
{
    public readonly EnvObjectReference type = VarEnvObjectReference.Variable("type");
    public DatumProcCollection RegistedProcedures { get; } = new();

    public dynamic self => VarEnvObjectReference.Wrap(this);
    public DatumExecutionContext ctx { get; private set; }

    public virtual void SetContext(DatumExecutionContext ctx)
    {
        this.ctx = ctx;
    }

    public virtual void _constructor()
    {
    }

    public DatumProcExecutionContext Invoke(DatumProcExecutionContext? caller, string name, Datum? usr,
        ProcArgumentList args, long? prec = null)
    {
        return RegistedProcedures.Invoke(caller, usr, this, ctx, name, args, prec);
    }

    public IEnumerable<ProcMetadata> EnumerateProcs(long prec = -1)
    {
        return RegistedProcedures.Enumerate(prec);
    }

    public bool HasProc(string name, long prec = -1)
    {
        return EnumerateProcs(prec).Any(p => p.Name == name);
    }

    public DatumProc GetProc(string name, long prec = -1)
    {
        return EnumerateProcs(prec).Where(p => p.Name == name).Single().Proc;
    }

    protected void RegisterProcedure(long prec, int declarationOrder, EnvObjectReference procedureTypeName)
    {
        var declType = ctx.ResolveType(procedureTypeName, declarationOrder);
        DoRegisterProcedure(prec, ((EnvObjectReference)ctx.NewAtomic(declType)).Get<DatumProc>());
    }

    protected virtual void DoRegisterProcedure(long prec, DatumProc proc)
    {
        RegistedProcedures.Register(prec, proc);
    }
}