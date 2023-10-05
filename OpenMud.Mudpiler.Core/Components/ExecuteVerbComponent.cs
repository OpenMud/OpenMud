namespace OpenMud.Mudpiler.Core.Components;

public struct ExecuteVerbComponent
{
    public readonly string SourceName;
    public readonly string DestinationName;
    public readonly string Verb;
    public readonly string[] Arguments;

    public ExecuteVerbComponent(string sourceName, string destinationName, string verb, string[] arguments)
    {
        SourceName = sourceName;
        DestinationName = destinationName;
        Verb = verb;
        Arguments = arguments;
    }
}