using DefaultEcs;

namespace OpenMud.Mudpiler.Core.Messages;

public struct CollisionMessage
{
    public readonly Entity Mover;
    public readonly Entity? Obstacle;

    public CollisionMessage(Entity mover, Entity? obstacle)
    {
        Mover = mover;
        Obstacle = obstacle;
    }
}