using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.AspNetCore.SignalR;
using OpenMud.Mudpiler.Net.Core.Hubs;

namespace OpenMud.Mudpiler.Net.Core;

public delegate void ServerRequest(RemoteGameDirector director);

public delegate void ClientCommand(IHubContext<WorldHub> ctx);

public interface IClientDispatcher
{
    IImmutableSet<string> ConnectedClients { get; }

    void Dispatch(IEnumerable<ClientCommand> command);
    void Dispatch(ClientCommand command);
}

public interface IClientReceiver
{
    IImmutableList<ServerRequest> DequeueRequests();
}

public interface IClientConnectionManager
{
    void AddClient(string clientId);
    void RemoveClient(string clientId);

    void Request(ServerRequest request);
    void Request(IEnumerable<ServerRequest> request);
}

public class ClientStateService : IClientConnectionManager, IClientDispatcher, IClientReceiver
{
    private readonly IHubContext<WorldHub> hubContext;
    private readonly ConcurrentDictionary<string, byte> KnownClients = new();
    private readonly ConcurrentQueue<ServerRequest> PendingRequests = new();

    public ClientStateService(IHubContext<WorldHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public void AddClient(string clientId)
    {
        KnownClients.TryAdd(clientId, 0);
    }

    public void RemoveClient(string clientId)
    {
        KnownClients.TryRemove(clientId, out _);
    }

    public void Request(IEnumerable<ServerRequest> request)
    {
        foreach (var r in request)
            PendingRequests.Enqueue(r);
    }

    public void Request(ServerRequest request)
    {
        Request(new[] { request });
    }

    public IImmutableSet<string> ConnectedClients => KnownClients.Keys.ToImmutableHashSet();

    public void Dispatch(ClientCommand command)
    {
        command(hubContext);
    }

    public void Dispatch(IEnumerable<ClientCommand> commands)
    {
        foreach (var c in commands)
            c(hubContext);
    }

    public IImmutableList<ServerRequest> DequeueRequests()
    {
        List<ServerRequest> requests = new();

        while (PendingRequests.TryDequeue(out var r))
            requests.Add(r);

        return requests.ToImmutableList();
    }
}