using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.Framework.Datums;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

public delegate void DatumEcho(DatumHandle receiver, string message);
public delegate void DatumSound(DatumHandle receiver, SoundInfo message);

internal class AtomicReadWrite : IRuntimeTypeBuilder
{
    private readonly DatumEcho echoHandler;
    private readonly DatumSound soundHandler;
    private readonly LogicDirectory logicDirectory;

    public AtomicReadWrite(LogicDirectory logicDirectory, DatumEcho echoHandler, DatumSound soundHandler)
    {
        this.echoHandler = echoHandler;
        this.soundHandler = soundHandler;
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
                    if (args[0].Type.IsAssignableTo(typeof(SoundInfo)))
                        return WriteSound(isMob, datum, args[0].Get<SoundInfo>());

                    return WriteAtomic(isMob, datum, args[0].GetOrDefault<string>("UNDEFINED SOURCE TYPE ERROR"));
                }
            )
        );
    }

    private EnvObjectReference WriteAtomic(bool isMob, Datum subject, string text)
    {
        if (isMob)
            echoHandler(logicDirectory[subject], text);

        return VarEnvObjectReference.NULL;
    }

    private EnvObjectReference WriteSound(bool isMob, Datum subject, SoundInfo text)
    {
        if (isMob)
            soundHandler(logicDirectory[subject], text);

        return VarEnvObjectReference.NULL;
    }
}