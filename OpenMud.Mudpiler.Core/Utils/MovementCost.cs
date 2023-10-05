namespace OpenMud.Mudpiler.Core.Utils;

public static class MovementCost
{
    public static float Compute(int deltaX, int deltaY)
    {
        return (Math.Abs(deltaX) + Math.Abs(deltaY)) * .2f;
    }
}