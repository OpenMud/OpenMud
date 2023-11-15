using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings
{
    public class Instant : IDmlProcAttribute
    {
        public Instant(int instant)
        {
            IsInstant = instant != 0;
        }

        public bool IsInstant { get; }

        public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
    }
}
