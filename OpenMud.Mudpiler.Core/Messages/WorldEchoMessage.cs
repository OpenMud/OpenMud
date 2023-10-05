namespace OpenMud.Mudpiler.Core.Messages;

public struct WorldEchoMessage
{
    public readonly string Message;

    public WorldEchoMessage(string message)
    {
        Message = message;
    }
}