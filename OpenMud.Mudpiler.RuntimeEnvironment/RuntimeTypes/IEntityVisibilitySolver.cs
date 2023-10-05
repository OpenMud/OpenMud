using System.Collections.Immutable;
using DefaultEcs;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public interface IEntityVisibilitySolver
{
    IImmutableDictionary<EntityHandle, float> ComputeVisible(World world, EntityHandle entity, int fieldOfView = -1);
}

public class NullVisibilitySolver : IEntityVisibilitySolver
{
    public IImmutableDictionary<EntityHandle, float> ComputeVisible(World world, EntityHandle entity,
        int fieldOfView = -1)
    {
        return new Dictionary<EntityHandle, float>().ToImmutableDictionary();
    }
}