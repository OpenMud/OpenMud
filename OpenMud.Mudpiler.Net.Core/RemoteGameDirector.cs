using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;

namespace OpenMud.Mudpiler.Net.Core;

public class RemoteGameDirector
{
    private readonly World world;

    public RemoteGameDirector(World world)
    {
        this.world = world;
    }

    public void RequestMovement(string clientId, string entityId, int deltaX, int deltaY)
    {
        bool isSubject(in IdentifierComponent i)
        {
            return i.Name == entityId;
        }

        var subject = world.GetEntities().With<IdentifierComponent>(isSubject).AsEnumerable().Single();
        //TODO: Should check the entity is actually owned by the client.
        if (deltaX == 0 && deltaY == 0)
        {
            subject.Remove<SlideComponent>();
        }
        else
        {
            var cost = MovementCost.Compute(deltaX, deltaY);
            subject.Set(new SlideComponent(deltaX, deltaY, cost, persist: true));
        }
    }

    public void DispatchCommand(string clientId, string source, string? subject, string command)
    {
        //TODO: Should check the entity is actually owned by the client.
        var e = world.CreateEntity();

        e.Set(new ParseCommandComponent(source, subject, command));
    }
}