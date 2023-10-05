namespace OpenMud.Mudpiler.Core.Components;

public class SlideComponent
{
    public readonly int DeltaX;
    public readonly int DeltaY;
    public readonly bool Persist;
    public readonly bool SkipLogicChecks;
    public readonly float TimeCost;

    public SlideComponent(int deltaX, int deltaY, float cost, bool skipLogicChecks = false, bool persist = false)
    {
        Persist = persist;
        DeltaX = deltaX;
        DeltaY = deltaY;
        TimeCost = cost;
        SkipLogicChecks = skipLogicChecks;
    }
}