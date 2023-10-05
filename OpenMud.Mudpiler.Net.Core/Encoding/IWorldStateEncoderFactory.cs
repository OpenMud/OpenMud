using DefaultEcs;

namespace OpenMud.Mudpiler.Net.Core.Encoding;

public interface IWorldStateEncoderFactory
{
    IWorldStateEncoder Create(World world);
}