using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Net.Core.Components;
using OpenMud.Mudpiler.Net.Core.Encoding;

namespace OpenMud.Mudpiler.Net.Core.Systems;

public class NetworkWorldSynchronizerSystem : ISystem<ServerFrame>
{
    private readonly IClientDispatcher clientState;

    private readonly HashSet<string> deltaDelete = new();
    private readonly IWorldStateEncoder encoder;

    private readonly HashSet<string> knownClients = new();

    private readonly World world;

    public NetworkWorldSynchronizerSystem(World world, IClientDispatcher clientState,
        IWorldStateEncoderFactory encoderFactory, bool useBuffer = false)
    {
        this.world = world;
        this.clientState = clientState;

        world.SubscribeComponentRemoved<IdentifierComponent>(DeltaDelete);
        world.SubscribeEntityDisposed(DeltaDelete);

        encoder = encoderFactory.Create(world);
    }

    public bool IsEnabled { get; set; } = true;

    public void Dispose()
    {
    }

    public void Update(ServerFrame frame)
    {
        if (!IsEnabled)
            return;

        var newClients = clientState.ConnectedClients.Except(knownClients).ToList();
        var lostClients = knownClients.Except(clientState.ConnectedClients).ToList();

        foreach (var c in newClients)
            OnNewClient(c);

        foreach (var c in lostClients)
            OnDisconnectedClient(c);

        knownClients.ExceptWith(lostClients);
        knownClients.UnionWith(newClients);

        if (deltaDelete.Any())
        {
            var deleteCommands = deltaDelete.Select(CreateDeleteCommand).ToList();
            clientState.Dispatch(deleteCommands);
            deltaDelete.Clear();
        }

        encoder.Encode(new StateTransmitter(clientState, frame.NetworkFrame));
    }

    private void OnNewClient(string clientId)
    {
        var e = world.CreateEntity();
        e.Set(new PlayerSessionComponent(clientId));

        encoder.InitializeScope(clientId);
    }

    private void OnDisconnectedClient(string clientId)
    {
        bool isSubject(in PlayerSessionComponent playerSession)
        {
            return playerSession.ConnectionId == clientId;
        }

        foreach (var e in world.GetEntities().With<PlayerSessionComponent>(isSubject).AsEnumerable())
            e.Dispose();

        encoder.DisposeScope(clientId);
    }

    private void DeltaDelete(in Entity entity)
    {
        if (!entity.Has<IdentifierComponent>())
            return;

        deltaDelete.Add(entity.Get<IdentifierComponent>().Name);
    }

    private void DeltaDelete(in Entity entity, in IdentifierComponent value)
    {
        DeltaDelete(entity);
    }

    private ClientCommand CreateDeleteCommand(string entityId, int networkFrame)
    {
        return hub => hub.Clients.All.SendCoreAsync("DeleteEntity", new object[] { networkFrame, entityId });
    }
}