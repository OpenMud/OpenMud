using System.Collections.Immutable;
using DefaultEcs;

namespace OpenMud.Mudpiler.Net.Core.Encoding.Components;

public class CascadeEncoder<T> : IBroadcastEntityComponentEncoder
{
    private CascadeEncoder(Type[] cascade)
    {
        Cascade = cascade.ToImmutableList();
    }

    public IImmutableList<Type> Cascade { get; }

    public void Accept(in Entity e)
    {
    }

    public void Encode(StateTransmitter hub)
    {
    }

    public static Func<IServiceProvider, BroadcastEntityComponentEncodeFactory<T>> Factory(Type[] cascade)
    {
        return _ => () => new CascadeEncoder<T>(cascade);
    }
}