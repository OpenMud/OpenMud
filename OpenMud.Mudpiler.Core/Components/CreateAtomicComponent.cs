using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Core.Components;

public struct CreateAtomicComponent
{
    public required string ClassName  { get; init; }
    public string? Identifier { get; init; } = null;
    public required int X  { get; init; }
    public required int Y  { get; init; }
    public int? Z  { get; init; } = null;
    
    public CreateAtomicComponent()
    {
        
    }
}