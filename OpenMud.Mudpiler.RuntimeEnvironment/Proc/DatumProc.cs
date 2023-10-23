using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public abstract class DatumProc : Datum
{
    public abstract string Name { get; }
    public abstract DatumProcExecutionContext Create();
    public abstract IDmlProcAttribute[] Attributes();
    public virtual ProcArgumentList DefaultArgumentList() => new ProcArgumentList();
}