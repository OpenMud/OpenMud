using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public abstract class DatumProc : Datum
{
    public abstract string Name { get; }
    public abstract DatumProcExecutionContext Create();
    public abstract IDmlProcAttribute[] Attributes();
    public virtual DatumProcExecutionContext CreateDefaultArgumentListBuilder() =>
        new PreparedDatumProcContext(() => VarEnvObjectReference.CreateImmutable(new ProcArgumentList()));
}