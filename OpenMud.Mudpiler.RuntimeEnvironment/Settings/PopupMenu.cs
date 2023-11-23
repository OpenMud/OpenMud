using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings
{
    public class PopupMenu : IDmlProcAttribute
    {
        public PopupMenu(int popupMenu)
        {
            Value = popupMenu != 0;
        }

        public bool Value { get; }

        public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
    }
}
