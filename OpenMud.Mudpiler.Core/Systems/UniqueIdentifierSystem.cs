using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(RequestIdentifierComponent))]
public class UniqueIdentifierSystem : AEntitySetSystem<float>
{
    public UniqueIdentifierSystem(World world, bool useBuffer = false) : base(world, useBuffer)
    {
        world.SubscribeComponentAdded<RequestIdentifierComponent>(On);
    }

    private void On(in Entity entity, in RequestIdentifierComponent value)
    {
        var searchName = value.Name;

        if (searchName == null)
            searchName = Guid.NewGuid().ToString();

        if (World.Any(e => e.Has<IdentifierComponent>() && e.Get<IdentifierComponent>().Name == searchName))
            throw new Exception("Error, identifier is not unique. Could not grant.");

        entity.Remove<RequestIdentifierComponent>();
        entity.Set(new IdentifierComponent(searchName));
    }
}