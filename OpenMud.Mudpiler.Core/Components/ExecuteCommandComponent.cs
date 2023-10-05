namespace OpenMud.Mudpiler.Core.Components;

public struct ExecuteCommandComponent
{
    public readonly string Source;
    public readonly string Verb;
    public readonly string[] Operands;
    public readonly string? Target = null;

    public ExecuteCommandComponent(string source, string? target, string verb, string[] operands)
    {
        Source = source;
        Verb = verb;
        Operands = operands;
        Target = target;
    }
}