using DefaultEcs;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Core.RuntimeTypes;

public interface IDmlFrameworkFactory
{
    IDmlFramework Create(World world, LogicDirectory logicLookup, IDmlTaskScheduler taskScheduler,
        IEntityVisibilitySolver entityVisibilitySolver);
}