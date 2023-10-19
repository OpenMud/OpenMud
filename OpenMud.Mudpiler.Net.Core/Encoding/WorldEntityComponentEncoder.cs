using System.Collections.Immutable;
using System.Reflection;
using DefaultEcs;
using Microsoft.Extensions.DependencyInjection;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Net.Core.Components;

namespace OpenMud.Mudpiler.Net.Core.Encoding;

public class WorldEntityComponentEncoder : IWorldStateEncoder
{
    private readonly World world;

    private readonly IImmutableList<Type> acceptedComponents;

    private readonly Dictionary<Type, Func<IBroadcastEntityComponentEncoder>> broadcastEncoderFactory;

    private readonly Dictionary<string, ClientScope> clientScopes = new();
    private readonly Dictionary<Type, Func<Entity, bool>> componentPresenceCheck;

    private readonly List<IEncodable> pendingBroadcast = new();

    private readonly Dictionary<string, List<IEncodable>> pendingClientBroadcast = new();
    private readonly Dictionary<Type, Func<IScopedEntityComponentEncoder>> scopedEncoderFactory;

    private readonly Dictionary<Type, Func<object>> scopedMessageEncoderFactory;
    private readonly Dictionary<Type, Func<object>> broadcastMessageEncoderFactory;

    public WorldEntityComponentEncoder(World world, IServiceProvider sp,
        IImmutableList<ServiceDescriptor> availableServices)
    {
        var scopedEncoderTypes =
            availableServices
                .Select(svc => svc.ServiceType)
                .Where(t => t.IsGenericType &&
                            t.GetGenericTypeDefinition() == typeof(ScopedEntityComponentEncoderFactory<>))
                .Select(t => t.GetGenericArguments().First());

        var broadcastEncoderTypes =
            availableServices
                .Select(svc => svc.ServiceType)
                .Where(t => t.IsGenericType &&
                            t.GetGenericTypeDefinition() == typeof(BroadcastEntityComponentEncodeFactory<>))
                .Select(t => t.GetGenericArguments().First());

        var broadcastMessageEncoderTypes =
            availableServices
                .Select(svc => svc.ServiceType)
                .Where(t => t.IsGenericType &&
                            t.GetGenericTypeDefinition() == typeof(BroadcastMessageEncodeFactory<>))
                .Select(t => t.GetGenericArguments().First());

        var scopedMessageEncoderTypes =
            availableServices
                .Select(svc => svc.ServiceType)
                .Where(t => t.IsGenericType &&
                            t.GetGenericTypeDefinition() == typeof(ScopedMessageEncoderFactory<>))
                .Select(t => t.GetGenericArguments().First());

        scopedEncoderFactory =
            scopedEncoderTypes
                .ToDictionary(
                    x => x,
                    x => new Func<IScopedEntityComponentEncoder>(
                        () =>
                        {
                            var factoryType = typeof(ScopedEntityComponentEncoderFactory<>).MakeGenericType(x);
                            var factory = sp.GetService(factoryType) as Delegate;

                            return (IScopedEntityComponentEncoder)factory.DynamicInvoke();
                        }
                    )
                );

        broadcastEncoderFactory =
            broadcastEncoderTypes
                .ToDictionary(
                    x => x,
                    x => new Func<IBroadcastEntityComponentEncoder>(
                        () =>
                        {
                            var factoryType = typeof(BroadcastEntityComponentEncodeFactory<>).MakeGenericType(x);
                            var factory = sp.GetService(factoryType) as Delegate;

                            return (IBroadcastEntityComponentEncoder)factory.DynamicInvoke();
                        }
                    )
                );


        broadcastMessageEncoderFactory =
            broadcastMessageEncoderTypes
                .ToDictionary(
                    x => x,
                    x => new Func<object>(
                        () =>
                        {
                            var factoryType = typeof(BroadcastMessageEncodeFactory<>).MakeGenericType(x);
                            var factory = sp.GetService(factoryType) as Delegate;

                            return factory.DynamicInvoke();
                        }
                    )
                );

        scopedMessageEncoderFactory =
            scopedMessageEncoderTypes
                .ToDictionary(
                    x => x,
                    x => new Func<object>(
                        () =>
                        {
                            var factoryType = typeof(ScopedMessageEncoderFactory<>).MakeGenericType(x);
                            var factory = sp.GetService(factoryType) as Delegate;

                            return factory.DynamicInvoke();
                        }
                    )
                );

        acceptedComponents =
            broadcastEncoderFactory.Keys.Concat(scopedEncoderFactory.Keys).Distinct().ToImmutableList();

        componentPresenceCheck =
            acceptedComponents
                .ToDictionary(
                    t => t,
                    t =>
                    {
                        var hasType = typeof(Entity).GetMethod("Has").MakeGenericMethod(t);

                        return new Func<Entity, bool>(
                            e => (bool)hasType.Invoke(e, null)
                        );
                    }
                );

        this.world = world;
        Subscribe(world);
    }

    public void DisposeScope(string client)
    {
        RemoveScope(client, null);
    }

    public void InitializeScope(string client)
    {
        foreach (var e in world.GetEntities().AsEnumerable())
            QueueTransmitEntityState(e, null, client);
    }

    public void Encode(StateTransmitter transmitter)
    {
        foreach (var p in pendingBroadcast)
            p.Encode(transmitter);

        foreach (var (client, pending) in pendingClientBroadcast)
        {
            var scopedTransmitter = transmitter.Scope(client);
            foreach (var p in pending) p.Encode(scopedTransmitter);
        }

        foreach (var c in clientScopes.Values)
            c.Encode(transmitter);

        pendingBroadcast.Clear();
        pendingClientBroadcast.Clear();
    }

    private string? GetOwningClient(in Entity entity)
    {
        if (!entity.Has<PlayerSessionOwnedComponent>() || !entity.Has<IdentifierComponent>())
            return null;

        return entity.Get<PlayerSessionOwnedComponent>().ConnectionId;
    }

    private string? GetOwningClient(string entityIdentifier)
    {
        bool matching(in IdentifierComponent i) => i.Name == entityIdentifier;

        var e = world.GetEntities().With<IdentifierComponent>(matching).AsEnumerable().FirstOrDefault();

        if (!e.IsAlive)
            return null;

        return GetOwningClient(entityIdentifier);
    }

    private IScopedEntityComponentEncoder? GetClientScope(in Entity entity, Type t)
    {
        var client = GetOwningClient(entity);

        if (client == null)
            return null;

        var identifier = entity.Get<IdentifierComponent>().Name;

        if (!clientScopes.ContainsKey(client))
            clientScopes[client] = new ClientScope(client, scopedEncoderFactory);

        return clientScopes[client].GetScopedEncoder(identifier, t);
    }

    private object CreateSubscribeAdd(Type type)
    {
        var funcType = typeof(ComponentAddedHandler<>).MakeGenericType(type);
        var method =
            typeof(WorldEntityComponentEncoder).GetMethod("SubscribeAdd",
                BindingFlags.NonPublic | BindingFlags.Instance);
        method = method.MakeGenericMethod(type);

        return Delegate.CreateDelegate(funcType, this, method);
    }

    private object CreateSubscribeChanged(Type type)
    {
        var funcType = typeof(ComponentChangedHandler<>).MakeGenericType(type);
        var method =
            typeof(WorldEntityComponentEncoder).GetMethod("SubscribeChanged",
                BindingFlags.NonPublic | BindingFlags.Instance);
        method = method.MakeGenericMethod(type);

        return Delegate.CreateDelegate(funcType, this, method);
    }

    private object CreateSubscribeOn(Type type)
    {
        var funcType = typeof(MessageHandler<>).MakeGenericType(type);
        var method =
            typeof(WorldEntityComponentEncoder).GetMethod("SubscribeMessage",
                BindingFlags.NonPublic | BindingFlags.Instance);
        method = method.MakeGenericMethod(type);

        return Delegate.CreateDelegate(funcType, this, method);
    }

    private Queue<Type> CreateInitialComponentScope(Entity entity, Type[]? typeConstraint)
    {
        var process = new Queue<Type>();
        var initialTypeScope = typeConstraint;

        if (initialTypeScope == null)
            initialTypeScope = acceptedComponents.ToArray();

        initialTypeScope = initialTypeScope
            .Where(componentPresenceCheck.ContainsKey)
            .Where(t => componentPresenceCheck[t](entity))
            .ToArray();

        foreach (var t in initialTypeScope)
            process.Enqueue(t);

        return process;
    }

    private void QueueTransmitMessage<T>(T message)
    {
        IMessageEncoder<T>? broadcastEncoder = null;
        if (scopedMessageEncoderFactory.TryGetValue(typeof(T), out var factory))
        {
            var scopedEncoder = (IScopedMessageEncoder<T>)factory();

            var entityScope = scopedEncoder.EntityMapper(message);

            if (entityScope == null)
                broadcastEncoder = scopedEncoder;
            else
            {
                var owningClient = GetOwningClient(entityScope);

                if (owningClient == null)
                    return;

                if (!pendingClientBroadcast.ContainsKey(owningClient))
                    pendingClientBroadcast.Add(owningClient, new List<IEncodable>());

                scopedEncoder.Accept(message);
                pendingClientBroadcast[owningClient].Add(scopedEncoder);
            }
        }

        if (broadcastEncoder == null)
        {
            if (!broadcastMessageEncoderFactory.TryGetValue(typeof(T), out var broadcastFactory))
                return;

            broadcastEncoder = (IMessageEncoder<T>)broadcastFactory();
            broadcastEncoder.Accept(message);

            pendingBroadcast.Add(broadcastEncoder);
        }
    }

    private void QueueTransmitEntityState(in Entity entity, Type[]? typeConstraint = null,
        string? clientConstraint = null)
    {
        var impacted = new HashSet<Type>();

        var process = CreateInitialComponentScope(entity, typeConstraint);

        while (process.Any())
        {
            var current = process.Dequeue();
            Type[] cascade;

            if (!impacted.Add(current))
                continue;

            if (!componentPresenceCheck.TryGetValue(current, out var hasComponent) || !hasComponent(entity))
                continue;

            if (broadcastEncoderFactory.TryGetValue(current, out var encoderFactory))
            {
                var encoder = encoderFactory();
                encoder.Accept(entity);

                var broadcastCollection = pendingBroadcast;


                if (clientConstraint != null &&
                    !pendingClientBroadcast.TryGetValue(clientConstraint, out broadcastCollection))
                {
                    broadcastCollection = new List<IEncodable>();
                    pendingClientBroadcast[clientConstraint] = broadcastCollection;
                }

                broadcastCollection.Add(encoder);

                cascade = encoder.Cascade.ToArray();
            }
            else
            {
                var owner = GetOwningClient(entity);

                if (clientConstraint != null && clientConstraint != owner)
                    continue;

                var scoped = GetClientScope(entity, current);

                if (scoped == null)
                    continue;

                scoped.Accept(entity);
                cascade = scoped.Cascade.ToArray();
            }

            foreach (var c in cascade)
                process.Enqueue(c);
        }
    }

    //These are used via reflection.
    private void SubscribeAdd<T>(in Entity entity, in T component)
    {
        QueueTransmitEntityState(entity, new[] { typeof(T) });
    }

    //These are used via reflection.
    private void SubscribeChanged<T>(in Entity entity, in T oldValue, in T newValue)
    {
        QueueTransmitEntityState(entity, new[] { typeof(T) });
    }

    //These are used via reflection.
    private void SubscribeMessage<T>(in T message)
    {
        QueueTransmitMessage(message);
    }

    private void Subscribe(World world)
    {
        var allEncodedTypes = broadcastEncoderFactory.Keys.Union(scopedEncoderFactory.Keys).Distinct();
        var allEncodedMessages = broadcastMessageEncoderFactory.Keys.Union(scopedMessageEncoderFactory.Keys).Distinct();
        foreach (var t in allEncodedTypes)
        {
            typeof(World).GetMethod("SubscribeComponentAdded")!.MakeGenericMethod(t)
                .Invoke(world, new[] { CreateSubscribeAdd(t) });

            typeof(World).GetMethod("SubscribeComponentChanged")!.MakeGenericMethod(t)
                .Invoke(world, new[] { CreateSubscribeChanged(t) });
        }

        foreach (var t in allEncodedMessages)
        {

            typeof(World).GetMethod("Subscribe")!.MakeGenericMethod(t)
                .Invoke(world, new[] { CreateSubscribeOn(t) });
        }
    }

    public void RemoveScope(string client, string? entity)
    {
        if (entity == null)
            clientScopes.Remove(client);
        else if (clientScopes.TryGetValue(client, out var scope))
            scope.Remove(entity);
    }

    private class ClientScope
    {
        private readonly string connection;

        private readonly Dictionary<Tuple<string, Type>, IScopedEntityComponentEncoder> entityScope = new();
        private readonly Dictionary<Type, Func<IScopedEntityComponentEncoder>> scopedEncoderFactory;

        public ClientScope(string connection,
            Dictionary<Type, Func<IScopedEntityComponentEncoder>> scopedEncoderFactory)
        {
            this.connection = connection;
            this.scopedEncoderFactory = scopedEncoderFactory;
        }

        public IScopedEntityComponentEncoder? GetScopedEncoder(string entity, Type type)
        {
            var key = Tuple.Create(entity, type);
            if (entityScope.TryGetValue(key, out var encoder))
                return encoder;

            if (!scopedEncoderFactory.TryGetValue(type, out var scopeFactory))
                return null;

            encoder = scopeFactory();

            encoder.Bind(connection);

            entityScope[key] = encoder;

            return encoder;
        }

        public void Remove(string entity)
        {
            var remove = entityScope.Where(x => x.Key.Item1 == entity).Select(x => x.Key).ToList();

            foreach (var r in remove)
                entityScope.Remove(r);
        }

        public void Encode(StateTransmitter transmitter)
        {
            foreach (var t in entityScope.Values)
                t.Encode(transmitter);
        }
    }
}