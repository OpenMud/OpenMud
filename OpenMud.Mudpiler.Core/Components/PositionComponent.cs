namespace OpenMud.Mudpiler.Core.Components;

public struct PositionComponent
{
    public readonly int x;
    public readonly int y;
    public readonly int z;

    public PositionComponent(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}