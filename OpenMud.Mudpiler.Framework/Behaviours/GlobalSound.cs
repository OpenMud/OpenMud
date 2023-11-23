using OpenMud.Mudpiler.Framework.Datums;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class GlobalSound : IRuntimeTypeBuilder
{
    private readonly ObjectInstantiator instantiator;

    public GlobalSound(ObjectInstantiator instantiator)
    {
        this.instantiator = instantiator;
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0,
            new ActionDatumProc("sound", args => sound(args[0, "file"], args[1, "repeat"], args[3, "channel"], args[4, "volume"])));
    }

    public bool AcceptsDatum(string target)
    {
        return DmlPath.IsDeclarationInstanceOfPrimitive(target, DmlPrimitive.Global);
    }

    public EnvObjectReference sound(EnvObjectReference music, EnvObjectReference repeatSound,
        EnvObjectReference channel, EnvObjectReference volume)
    {
        var sound = instantiator(typeof(SoundInfo), new ProcArgumentList(music));
        var soundRaw = sound.Get<SoundInfo>();

        soundRaw.repeat.Assign(DmlEnv.AsLogical(repeatSound) ? 1 : 0);
        soundRaw.channel.Assign(DmlEnv.AsNumeric(channel));

        soundRaw.volume.Assign(DmlEnv.AsNumeric(volume.GetOrDefault(100)));

        return sound;
    }
}