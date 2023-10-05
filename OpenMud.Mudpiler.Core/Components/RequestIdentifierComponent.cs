namespace OpenMud.Mudpiler.Core.Components;

public struct RequestIdentifierComponent
{
    public readonly string? Name;

    public RequestIdentifierComponent()
    {
        Name = null;
    }

    public RequestIdentifierComponent(string? name)
    {
        Name = name;
    }
}