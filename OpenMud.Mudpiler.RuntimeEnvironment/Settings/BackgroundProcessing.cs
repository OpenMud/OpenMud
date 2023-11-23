using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings
{
    //To avoid lag from procedures that hog the CPU for too long, you can turn on background processing
    public class BackgroundProcessing : IDmlProcAttribute
    {
        public BackgroundProcessing(int background)
        {
            Background = background != 0;
        }

        public bool Background { get; }

        public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
    }
}
