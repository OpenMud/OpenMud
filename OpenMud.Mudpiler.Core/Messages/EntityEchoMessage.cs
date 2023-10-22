namespace OpenMud.Mudpiler.Core.Messages;

public struct EntityEchoMessage
{
    public readonly string Identifier;
    public readonly string Name;
    public readonly string Message;

    public EntityEchoMessage(string identifier, string name, string message)
    {
        Identifier = identifier;
        Name = name;
        Message = message;
    }
}