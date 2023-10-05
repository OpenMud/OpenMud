namespace OpenMud.Mudpiler.Core.Messages;

public struct EntityEchoMessage
{
    public readonly Guid Id;
    public readonly string Message;

    public EntityEchoMessage(Guid id, string message)
    {
        Id = id;
        Message = message;
    }
}