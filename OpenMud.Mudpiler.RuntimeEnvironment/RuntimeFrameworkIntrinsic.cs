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
        public static readonly string ADDTEXT = "addtext";
        public static readonly string INDIRECT_CALL = "_indirect_call";
        public static readonly string GENERATE_RANGE = "_generate_range";
        public static readonly string PICK_WEIGHTED = "_weighted_pick";
        public static readonly string INDIRECT_NEW = "indirect_new";
        public static readonly string INDIRECT_ISTYPE = "indirect_istype";
        public static readonly string FIELD_LIST_INIT = "_field_list_initialize";
        public static readonly string THROW_EXCEPTION = "__throwException";
    }
}
