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
    public class ReservedKeywordsTest
    {
        [DatapointSource]
        public string[] Reserved= new string[]
        {
            "event",
            "internal",
            "virtual"
        };

        [Theory]
        public void TestEventKeywordField(string keyword)
        {
            var dmlCode =
                $@"
/mob/test
    var/{keyword} = ""testname""
    proc
        testproc()
            {keyword} = {keyword} + ""appended""
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var m = system.CreateAtomic("/mob/test");

            Assert.IsTrue(m[keyword] == "testname");

            m.ExecProc("testproc").CompleteOrException();

            Assert.IsTrue(m[keyword] == "testnameappended");

        }


        [Theory]
        public void TestEventKeywordVariable(string keyword)
        {
            var dmlCode =
                $@"
/mob/test
    proc
        testproc()
            var {keyword} = ""testname""
            {keyword} = {keyword} + ""appended""

            return {keyword}
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var m = system.CreateAtomic("/mob/test");
            var r = (string)m.ExecProc("testproc").CompleteOrException();

            Assert.IsTrue(r == "testnameappended");

        }
    }
}
