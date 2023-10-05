namespace OpenMud.Mudpiler.Net.Core.Encoding;

public class StateTransmitter
{
    private readonly long frame;
    private readonly IClientDispatcher hub;
    private readonly string? implicitClientScope;

    public StateTransmitter(IClientDispatcher hub, long frame, string? implicitClientScope = null)
    {
        this.frame = frame;
        this.hub = hub;
        this.implicitClientScope = implicitClientScope;
    }

    public StateTransmitter Scope(string impicitScope)
    {
        return new StateTransmitter(hub, frame, implicitClientScope);
    }

    public void Transmit(string name, params object[] args)
    {
        if (implicitClientScope != null)
            TransmitScoped(implicitClientScope, name, args);
        else
            hub.Dispatch(ctx => ctx.Clients.All.SendCoreAsync(name, args.Prepend(frame).ToArray()));
    }

    public void TransmitScoped(string client, string name, params object[] args)
    {
        if (implicitClientScope != null && implicitClientScope != client)
            return;

        hub.Dispatch(ctx => ctx.Clients.Client(client).SendCoreAsync(name, args.Prepend(frame).ToArray()));
    }
}