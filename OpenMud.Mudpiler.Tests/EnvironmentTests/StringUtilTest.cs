using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    public class StringUtilTest
    {
        [DatapointSource]
        public Tuple<string, string, int>[] TEST_DATA =
        {
            Tuple.Create("HeLlO WoRlD", "WORLD", 7),
            Tuple.Create("HeLlO", "WORLD", 0),
            Tuple.Create("", "", 0),
            Tuple.Create("hello", "", 0),
            Tuple.Create("hello hello", "hello", 1),
        };

        [Theory]
        public void findtext_test(Tuple<string, string, int> data)
        {
            var dmlCode =
                @$"
/proc/test0()
    return findtext(""{data.Item1}"", ""{data.Item2}"")
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r0 = system.Global.ExecProc("test0").CompleteOrException();

            Assert.IsTrue(DmlEnv.AsNumeric(r0) == data.Item3);
        }
    }
}
