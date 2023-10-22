using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Net.Core.Encoding.Messages
{
    public sealed class SimpleScopedMessageEncoder<T> : IScopedMessageEncoder<T>
    {
        public Func<T, string?> EntityMapper { get; private set; }

        private readonly List<T> buffer = new();
        private string? client;
        private readonly string setter;

        public SimpleScopedMessageEncoder(string setter, Func<T, string?> entityAssociationLookup)
        {
            EntityMapper = entityAssociationLookup;
            this.setter = setter;
        }

        public void Accept(in T e)
        {
            this.buffer.Add(e);
        }

        public void Encode(StateTransmitter hub)
        {
            if (this.client == null)
                throw new Exception("Encoder is not bound.");


            foreach (var m in this.buffer)
            {
                if(m != null)
                    hub.Transmit(setter, m);
            }

            this.buffer.Clear();
        }

        public void Bind(string connection)
        {
            if (this.client != null)
                throw new Exception("Encoder is already bound.");

            this.client = connection;
        }

        public static Func<IServiceProvider, ScopedMessageEncoderFactory<T>> Factory(string setter, Func<T, string?> entityAssociationLookup)
        {
            return _ => () => new SimpleScopedMessageEncoder<T>(setter, entityAssociationLookup);
        }
    }
}
