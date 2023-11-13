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
    public class MixedIndentationTest
    {
        [Test]
        public void TestSpacesWithTabs()
        {
            var dmlCode =
                $@"
/proc/test()
  var x = 10
  var y = 10
  return x * y

/proc/test2()
{"\t"}var x = 20
{"\t"}var y = 20
{"\t"}return x * y
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            Assert.IsTrue(100 == (int)system.Global.ExecProc("test").CompleteOrException());

            Assert.IsTrue(400 == (int)system.Global.ExecProc("test2").CompleteOrException());
        }
    }
}
