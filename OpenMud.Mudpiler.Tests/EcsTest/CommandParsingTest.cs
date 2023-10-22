using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Core.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Tests.EcsTest
{
    internal class CommandParsingTest
    {
        private static void Stabalize(TestGame w)
        {
            for (var i = 0; i < 100; i++)
                w.Update(1);
        }

        [Test]
        public void ArgConsumeTextTest()
        {
            var dmlCode =
                @$"
/mob
    verb
        say(a as text, w as num)
            var l = w
            l += 1
            world << a
            world << l
";
            var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
            var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

            Stabalize(g);

            var name = $"/mob";
            var instance = g.Create(name, true);

            Stabalize(g);

            g.ExecuteCommand(instance, instance, "say \"test text 15\" 5");

            Stabalize(g);

            Assert.IsTrue(g.WorldMessages.Count == 2);
            Assert.IsTrue(g.WorldMessages[0] == "test text 15");
            Assert.IsTrue(g.WorldMessages[1] == "6");
        }

        [Test]
        public void ArgConsumeTextTestWithEscapeSequence()
        {
            var dmlCode =
                @$"
/mob
    verb
        say(a as text, w as num)
            var l = w
            l += 1
            world << a
            world << l
";
            var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
            var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

            Stabalize(g);

            var name = $"/mob";
            var instance = g.Create(name, true);

            Stabalize(g);

            g.ExecuteCommand(instance, instance, "say \"test \\\"text\\\" 15\" 5");

            Stabalize(g);

            Assert.IsTrue(g.WorldMessages.Count == 2);
            Assert.IsTrue(g.WorldMessages[0] == "test \"text\" 15");
            Assert.IsTrue(g.WorldMessages[1] == "6");
        }
    }
}
