namespace OpenMud.Mudpiler.Core.Messages;

public struct CommandRejectionMessage
{
    public readonly string Source;
    public readonly string Reason;

    public CommandRejectionMessage(string source, string reason)
    {
        Source = source;
        Reason = reason;
    }
}