namespace OpenMud.Mudpiler.Net.Core.Components;

public readonly struct PlayerSessionOwnedComponent
{
    public readonly string ConnectionId;

    public PlayerSessionOwnedComponent(string connectionId)
    {
        ConnectionId = connectionId;
    }
}