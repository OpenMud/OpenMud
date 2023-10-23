using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment
{
    //List of symbols that are essential / directly encoded into a program as part of the compile pipeline:
    //for example, "hello [person]" is compiled into text("hello []", person), therefore text is a runtime intrinsic function
    public static class RuntimeFrameworkIntrinsic
    {
        public static readonly string TEXT = "text";
        public static readonly string INDIRECT_CALL = "_indirect_call";
    }
}
