using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.TypeSolver
{
    internal interface IDmlTypeLibrary
    {
        public Type? TryLookup(string fullyQualifiedTypeName);
        public string? TryLookup(Type t);
    }
}
