namespace OpenMud.Mudpiler.Core.Components;

public struct PathFindingComponent
{
    public readonly string? TargetEntity;
    public readonly int DestinationX;
    public readonly int DestinationY;
    public readonly int HaltDistance;

    public PathFindingComponent(int destinationX, int destinationY, int haltDistance = 0)
    {
        TargetEntity = null;
        DestinationX = destinationX;
        DestinationY = destinationY;
        HaltDistance = haltDistance;
    }

    public PathFindingComponent(string targetEntity, int haltDistance = 0)
    {
        TargetEntity = targetEntity;
        DestinationX = 0;
        DestinationY = 0;
        HaltDistance = haltDistance;
    }
}