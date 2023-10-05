using System.Collections.Immutable;
using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Net.Core.Encoding.Components;

public struct EntityVisibilitySet
{
    public readonly string[] Entities;

    public EntityVisibilitySet(string[] entities)
    {
        Entities = entities;
    }
}

public sealed class EntityVisibilityEncoder : IScopedEntityComponentEncoder
{
    private string? connection;

    private string? identifier;

    private readonly HashSet<string> knownVisible = new();
    private readonly HashSet<string> pendingDispatchNotVisible = new();

    private readonly HashSet<string> pendingDispatchVisible = new();
    public IImmutableList<Type> Cascade => ImmutableList<Type>.Empty;

    public void Bind(string connection)
    {
        if (this.connection != null)
            throw new Exception("Can only be bound to a single connection.");

        this.connection = connection;
    }

    public void Accept(in Entity e)
    {
        if (!e.Has<IdentifierComponent>())
            return;

        var curSubject = e.Get<IdentifierComponent>().Name;

        if (identifier == null)
            identifier = curSubject;

        if (identifier != curSubject)
            throw new Exception("Encoded must be bound to a single entity for the lifetime of the encoder.");

        var currentVisible = e.Get<EntityVisibilityComponent>().VisibleEntities;

        var newVisible = currentVisible.Except(knownVisible).ToHashSet();
        var lostVisible = knownVisible.Except(currentVisible).ToHashSet();

        knownVisible.ExceptWith(lostVisible);
        knownVisible.UnionWith(newVisible);

        pendingDispatchNotVisible.UnionWith(lostVisible);
        pendingDispatchNotVisible.ExceptWith(newVisible);

        pendingDispatchVisible.UnionWith(newVisible);
        pendingDispatchVisible.ExceptWith(lostVisible);
    }

    public void Encode(StateTransmitter hub)
    {
        if (connection == null)
            throw new Exception("Not bound to a connection.");

        if (identifier == null)
            return;

        if (pendingDispatchVisible.Any())
            hub.TransmitScoped(connection, "SetVisible", identifier,
                new EntityVisibilitySet(pendingDispatchVisible.ToArray()));

        if (pendingDispatchNotVisible.Any())
            hub.TransmitScoped(connection, "SetInvisible", identifier,
                new EntityVisibilitySet(pendingDispatchNotVisible.ToArray()));

        pendingDispatchNotVisible.Clear();
        pendingDispatchVisible.Clear();
    }

    public static Func<IServiceProvider, ScopedEntityComponentEncoderFactory<EntityVisibilityComponent>> Factory()
    {
        return _ => () => new EntityVisibilityEncoder();
    }
}