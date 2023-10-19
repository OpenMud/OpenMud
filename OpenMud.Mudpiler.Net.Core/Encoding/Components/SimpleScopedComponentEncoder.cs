using System.Collections.Immutable;
using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Net.Core.Encoding.Components;

public sealed class SimpleScopedComponentEncoder<T> : IScopedEntityComponentEncoder
{
    private readonly string setterName;
    private Action<StateTransmitter, string>? action;
    private string? connection;

    private SimpleScopedComponentEncoder(string setterName, Type[]? cascade = null)
    {
        this.setterName = setterName;

        if (cascade != null)
            Cascade = cascade.ToImmutableList();
    }

    public IImmutableList<Type> Cascade { get; } = ImmutableList<Type>.Empty;

    public void Accept(in Entity e)
    {
        if (!e.Has<IdentifierComponent>() || !e.Has<T>())
            return;

        var name = e.Get<IdentifierComponent>().Name;
        var val = e.Get<T>()!;

        action = (hub, connection) => hub.TransmitScoped(connection, setterName, name, val);
    }

    public void Bind(string connection)
    {
        if (this.connection != null)
            throw new Exception("Already bound to a connection.");

        this.connection = connection;
    }

    public void Encode(StateTransmitter hub)
    {
        if (connection == null)
            throw new Exception("Not bound to a connection.");

        if (action == null)
            return;

        action(hub, connection);

        action = null;
    }

    public static Func<IServiceProvider, ScopedEntityComponentEncoderFactory<T>> Factory(string setter,
        Type[]? cascade = null)
    {
        return _ => () => new SimpleScopedComponentEncoder<T>(setter, cascade);
    }
}