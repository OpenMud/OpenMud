using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public interface IRuntimeTypeBuilder
{
    bool AcceptsDatum(string target);

    bool AcceptsProc(string target)
    {
        return false;
    }

    void Build(DatumHandle handle, Datum datum, DatumProcCollection procedureCollection);
}