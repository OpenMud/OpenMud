using System.Collections.Immutable;
using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Net.Core.Encoding.Components;

public delegate bool EncoderFilter(in Entity e);

public sealed class SimpleBroadcastEncoder<T> : IBroadcastEntityComponentEncoder
{
    private readonly EncoderFilter? filter;
    private readonly string setterName;
    private Action<StateTransmitter>? action;

    private SimpleBroadcastEncoder(string setterName, Type[]? cascade, EncoderFilter? filter)
    {
        this.setterName = setterName;
        this.filter = filter;

        if (cascade != null)
            Cascade = cascade.ToImmutableList();
    }

    public IImmutableList<Type> Cascade { get; } = ImmutableList<Type>.Empty;

    public void Accept(in Entity e)
    {
        if (!e.Has<IdentifierComponent>() || !e.Has<T>())
            return;

        if (filter != null && !filter(e))
            return;

        var name = e.Get<IdentifierComponent>().Name;
        var val = e.Get<T>()!;

        action = hub => hub.Transmit(setterName, name, val);
    }

    public void Encode(StateTransmitter hub)
    {
        if (action == null)
            return;

        action(hub);

        action = null;
    }

    public static Func<IServiceProvider, BroadcastEntityComponentEncodeFactory<T>> Factory(string setter,
        Type[]? cascade = null, EncoderFilter? filter = null)
    {
        return _ => () => new SimpleBroadcastEncoder<T>(setter, cascade, filter);
    }
}