using OpenMud.Mudpiler.Framework.Datums;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class WorldReadWrite : IRuntimeTypeBuilder
{
    private readonly DatumEcho echoHandler;
    private readonly DatumSound soundHandler;

    public WorldReadWrite(DatumEcho echoHandler, DatumSound soundHandler)
    {
        this.echoHandler = echoHandler;
        this.soundHandler = soundHandler;
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.World);
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0,
            new ActionDatumProc(
                "Operator_Input",
                new[]
                {
                    new BinOpOverride(DmlBinary.BitShiftLeft)
                },
                args =>
                {
                    if (args[0].Type.IsAssignableTo(typeof(SoundInfo)))
                        soundHandler(e, args[0].Get<SoundInfo>());
                    else
                        echoHandler(e, args[0].Get<object>().ToString());

                    return VarEnvObjectReference.NULL;
                }
            )
        );
    }
}