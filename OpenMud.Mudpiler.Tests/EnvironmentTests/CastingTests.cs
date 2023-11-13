using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    public class CastingTests
    {
        [Test]
        public void CastTextToNumericTest()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    return (""123"" as num) + (""321"" as num)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test0").CompleteOrException();
            Assert.IsTrue(r == 444);
        }

        [Test]
        public void CastNumericToTextTest()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    return (123 + 321) as text
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (string)system.Global.ExecProc("test0").CompleteOrException();
            Assert.IsTrue(r == "444");
        }

    }
}
