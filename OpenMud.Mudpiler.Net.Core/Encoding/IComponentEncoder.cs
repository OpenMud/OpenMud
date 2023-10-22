using System.Collections.Immutable;
using DefaultEcs;

namespace OpenMud.Mudpiler.Net.Core.Encoding;

public interface IComponentEncoder : IEncodable
{
    IImmutableList<Type> Cascade { get; }
    void Accept(in Entity e);
}

public delegate IBroadcastEntityComponentEncoder BroadcastEntityComponentEncodeFactory<T>();

public delegate IScopedEntityComponentEncoder ScopedEntityComponentEncoderFactory<T>();

public interface IScopedEntityComponentEncoder : IComponentEncoder
{
    void Bind(string connection);
}

public interface IBroadcastEntityComponentEncoder : IComponentEncoder
{
}