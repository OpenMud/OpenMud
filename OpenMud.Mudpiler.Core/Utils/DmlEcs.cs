using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Core.Utils;

public static class DmlEcs
{
    public static IEnumerable<Entity> FindEntities(World world, LogicDirectory logicLookup, EntityHandle entityHandle)
    {
        var logicInstanceId = logicLookup[entityHandle];

        bool matchingInstanceId(in LogicIdentifierComponent c)
        {
            return c.LogicInstanceId == logicInstanceId;
        }

        return world.GetEntities().With<LogicIdentifierComponent>(matchingInstanceId).AsEnumerable();
    }

    public static Entity FindEntity(World world, LogicDirectory logicLookup, EntityHandle entityHandle)
    {
        return FindEntities(world, logicLookup, entityHandle).Single();
    }
}