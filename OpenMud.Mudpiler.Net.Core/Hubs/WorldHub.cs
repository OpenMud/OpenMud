using Microsoft.AspNetCore.SignalR;

namespace OpenMud.Mudpiler.Net.Core.Hubs;

public class WorldHub : Hub
{
    private readonly IClientConnectionManager clientState;

    public WorldHub(IClientConnectionManager clientState)
    {
        this.clientState = clientState;
    }

    public override Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        clientState.AddClient(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        clientState.RemoveClient(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public void RequestMovement(string identifier, int deltaX, int deltaY)
    {
        var connectionId = Context.ConnectionId;

        clientState.Request(
            director => director.RequestMovement(connectionId, identifier, deltaX, deltaY)
        );
    }

    public void ClearMovementRequest(string identifier)
    {
        RequestMovement(identifier, 0, 0);
    }

    public void ExecuteVerb(string sourceIdentifier, string? targetIdentifier, string command)
    {
        var connectionId = Context.ConnectionId;

        clientState.Request(
            director => director.DispatchCommand(connectionId, sourceIdentifier, targetIdentifier, command)
        );
    }
}