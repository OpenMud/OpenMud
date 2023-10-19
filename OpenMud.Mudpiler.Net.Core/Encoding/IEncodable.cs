using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Net.Core.Encoding
{
    public interface IEncodable
    {
        void Encode(StateTransmitter hub);
    }
}
