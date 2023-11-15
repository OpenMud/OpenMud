using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings
{
    public class Hidden : IDmlProcAttribute
    {
        public Hidden(int hidden)
        {
            IsHidden = hidden != 0;
        }

        public bool IsHidden { get; }

        public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
    }
}
