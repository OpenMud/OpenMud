using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public abstract class DmlCoord : Datum
{
    public abstract EnvObjectReference x { get; }
    public abstract EnvObjectReference y { get; }
    public abstract EnvObjectReference z { get; }
}