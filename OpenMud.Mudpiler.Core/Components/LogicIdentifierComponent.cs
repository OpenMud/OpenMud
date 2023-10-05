namespace OpenMud.Mudpiler.Core.Components;

public struct LogicIdentifierComponent
{
    public readonly Guid LogicInstanceId;

    public LogicIdentifierComponent(Guid handle)
    {
        LogicInstanceId = handle;
    }
}