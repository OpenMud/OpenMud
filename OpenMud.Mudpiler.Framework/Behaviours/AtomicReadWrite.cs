using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

public delegate void DatumEcho(DatumHandle receiver, string message);

internal class AtomicReadWrite : IRuntimeTypeBuilder
{
    private readonly DatumEcho echoHandler;
    private readonly LogicDirectory logicDirectory;

    public AtomicReadWrite(LogicDirectory logicDirectory, DatumEcho echoHandler)
    {
        this.echoHandler = echoHandler;
        this.logicDirectory = logicDirectory;
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.Atom);
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        var isMob = RuntimeTypeResolver.InheritsBaseTypeDatum(datum.type.Get<string>(), DmlPrimitiveBaseType.Mob);
        procedureCollection.Register(0,
            new ActionDatumProc(
                "Operator_Input",
                new[]
                {
                    new BinOpOverride(DmlBinary.BitShiftLeft)
                },
                (args, datum) =>
                {
                    return WriteAtomic(isMob, datum, args[0].GetOrDefault<string>("UNDEFINED SOURCE TYPE ERROR"));
                }
            )
        );
    }

    private EnvObjectReference WriteAtomic(bool isMob, Datum subject, string text)
    {
        if (echoHandler != null && isMob)
            echoHandler(logicDirectory[subject], text);

        return VarEnvObjectReference.NULL;
    }
}