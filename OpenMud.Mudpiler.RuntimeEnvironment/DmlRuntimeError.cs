using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment
{
    public class DmlRuntimeError : Exception
    {
        public DmlRuntimeError(string reason) : base(reason)
        {

        }
    }
}
