using DefaultEcs;
using OpenMud.Mudpiler.Core.RuntimeTypes;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Framework;

public class BaseDmlFrameworkFactory : IDmlFrameworkFactory
{
    public IDmlFramework Create(World world, LogicDirectory logicLookup, IDmlTaskScheduler taskScheduler,
        IEntityVisibilitySolver entityVisibilitySolver)
    {
        return new BaseDmlFramework(world, logicLookup, taskScheduler, entityVisibilitySolver);
    }
}