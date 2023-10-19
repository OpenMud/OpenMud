using System.Collections.Immutable;
using DefaultEcs;

namespace OpenMud.Mudpiler.Net.Core.Encoding;

public interface IMessageEncoder<T> : IEncodable
{
    void Accept(in T e);
}

public delegate IBroadcastMessageEncoder<T> BroadcastMessageEncodeFactory<T>();

public delegate IScopedMessageEncoder<T> ScopedMessageEncoderFactory<T>();

public interface IScopedMessageEncoder<T> : IMessageEncoder<T>
{
    Func<T, string?> EntityMapper { get; }
    void Bind(string connection);
}

public interface IBroadcastMessageEncoder<T> : IMessageEncoder<T>
{
}