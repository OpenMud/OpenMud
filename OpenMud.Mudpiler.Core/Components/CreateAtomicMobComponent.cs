namespace OpenMud.Mudpiler.Core.Components;

public struct CreateAtomicMobComponent
{
    public string? Identifier { get; init; } = null;
    public int X { get; init; } = 0;
    public int Y  { get; init; } = 0;
    public int? Z  { get; init; } = null;
    
    public CreateAtomicMobComponent()
    {
        
    }
}