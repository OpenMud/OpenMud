using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Tests.EcsTest
{
    public class WorldEchoTest
    {
        private static void Stabalize(TestGame w)
        {
            for (var i = 0; i < 100; i++)
                w.Update(1);
        }

        [Test]
        public void FormattedWorldEchoTest()
        {
            var testCode =
                @$"
/proc/speakto_test(a, b)
    world << ""Hello [a], [b + 10]""
";
            var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null, EnvironmentConstants.BUILD_MACROS);
            var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
            var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

            Stabalize(g);

            g.Environment.Global.ExecProc("speakto_test", "bob", 99).CompleteOrException();

            Stabalize(g);

            Assert.IsTrue(g.WorldMessages.Single() == "Hello bob, 109");
        }

        [Test]
        public void FormattedWorldEchoArrayIndexTest()
        {
            var testCode =
            @$"
/proc/speakto_test()
    var q = list(1,2,3,4,5)
    world << ""Hello [q[2] + 110]""

";

            var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null, EnvironmentConstants.BUILD_MACROS);
            var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
            var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

            Stabalize(g);

            g.Environment.Global.ExecProc("speakto_test").CompleteOrException();

            Stabalize(g);

            Assert.IsTrue(g.WorldMessages.Single() == "Hello 112");
        }
    }
}
