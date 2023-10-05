using DefaultEcs;
using DefaultEcs.System;

namespace OpenMud.Mudpiler.Net.Core.Systems;

public class NetworkWorldRequestRealizerSystem : ISystem<ServerFrame>
{
    private readonly IClientReceiver clientReceiver;
    private readonly RemoteGameDirector director;

    public NetworkWorldRequestRealizerSystem(World world, IClientReceiver clientReceiver)
    {
        director = new RemoteGameDirector(world);
        this.clientReceiver = clientReceiver;
    }

    public bool IsEnabled { get; set; } = true;

    public void Dispose()
    {
    }

    public void Update(ServerFrame state)
    {
        foreach (var c in clientReceiver.DequeueRequests())
            c(director);
    }
}