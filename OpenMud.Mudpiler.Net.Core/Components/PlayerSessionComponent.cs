namespace OpenMud.Mudpiler.Net.Core.Components;

public struct PlayerSessionComponent
{
    public readonly string ConnectionId;

    public PlayerSessionComponent(string connectionId)
    {
        ConnectionId = connectionId;
    }
}