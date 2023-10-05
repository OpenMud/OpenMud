using DefaultEcs;

namespace OpenMud.Mudpiler.Core.Messages;

public struct PathingFailedMessage
{
    public readonly Entity Mover;

    public PathingFailedMessage(Entity mover)
    {
        Mover = mover;
    }
}