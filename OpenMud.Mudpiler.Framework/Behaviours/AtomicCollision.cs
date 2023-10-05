using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class AtomicCollision : IRuntimeTypeBuilder
{
    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.Atom);
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0,
            new ActionDatumProc(
                "Enter",
                (args, datum) => Enter((Atom)datum, args[0])
            )
        );
    }

    private EnvObjectReference Enter(Atom d, EnvObjectReference args)
    {
        if (DmlEnv.AsDecimal(d.density) != 0 && DmlEnv.AsDecimal(args.Get<Atom>().density) != 0)
            return VarEnvObjectReference.CreateImmutable(0);

        return VarEnvObjectReference.CreateImmutable(1);
    }
}