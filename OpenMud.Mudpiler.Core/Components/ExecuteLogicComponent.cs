namespace OpenMud.Mudpiler.Core.Components;

public struct ExecuteLogicComponent
{
    public readonly string? SourceName;
    public readonly string DestinationName;
    public readonly string MethodName;
    public readonly object[] Arguments;

    public ExecuteLogicComponent(string sourceName, string destinationName, string methodName, object[] arguments)
    {
        SourceName = sourceName;
        MethodName = methodName;
        Arguments = arguments;
        DestinationName = destinationName;
    }

    public ExecuteLogicComponent(string destinationName, string methodName, object[] arguments)
    {
        SourceName = null;
        MethodName = methodName;
        Arguments = arguments;
        DestinationName = destinationName;
    }
}