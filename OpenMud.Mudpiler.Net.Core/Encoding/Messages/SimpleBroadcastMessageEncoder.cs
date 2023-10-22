using OpenMud.Mudpiler.Net.Core.Encoding.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Net.Core.Encoding.Messages
{
    public sealed class SimpleBroadcastMessageEncoder<T> : IBroadcastMessageEncoder<T>
    {
        private readonly string setterName;
        private readonly List<T> buffer = new();

        public SimpleBroadcastMessageEncoder(string setterName)
        {
            this.setterName = setterName;
        }

        public void Accept(in T e)
        {
            buffer.Add(e);
        }

        public void Encode(StateTransmitter hub)
        {
            foreach (var b in buffer)
            {
                if(b != null)
                    hub.Transmit(this.setterName, b);
            }

            buffer.Clear();
        }

        public static Func<IServiceProvider, BroadcastMessageEncodeFactory<T>> Factory(string setter)
        {
            return _ => () => new SimpleBroadcastMessageEncoder<T>(setter);
        }
    }
}
