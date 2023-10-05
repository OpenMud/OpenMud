using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

public class ProcBasic : IRuntimeTypeBuilder
{
    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0, new ActionDatumProc("New", (args, self) => New(self, args)));
    }

    public bool AcceptsProc(string target)
    {
        return true;
    }

    public bool AcceptsDatum(string target)
    {
        return false;
    }

    public EnvObjectReference New(Datum self, ProcArgumentList args)
    {
        if (args.MaxPositionalArgument == 0)
            return VarEnvObjectReference.CreateImmutable(self);

        var hostAtom = args.Get(0).Get<Atom>();
        var strName = args.Get(1)?.GetOrDefault<string>(null);
        var strDesc = args.Get(2)?.GetOrDefault<string>(null);

        var proc = (DmlDatumProc)self;

        hostAtom.RegisterExternalVerb(proc, strName, strDesc);

        return VarEnvObjectReference.CreateImmutable(self);
    }
}