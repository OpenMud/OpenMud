using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings
{
    public class FieldTypeHint : Attribute
    {
        public readonly string TypeName;
        public FieldTypeHint(string typeName) {
            TypeName = typeName;
        }
    }
}
