using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Tests.EcsTest;

public class DatumEcho
{
    [DatapointSource] public Tuple<string, bool>[] testSubjects =
    {
        Tuple.Create("obj", false),
        Tuple.Create("area", false),
        Tuple.Create("turf", false),
        Tuple.Create("mob", true),
        Tuple.Create("atom", false)
    };

    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }

    [Theory]
    public void SpeakToTest(Tuple<string, bool> dp)
    {
        var subject = dp.Item1;
        var dmlCode =
            @$"
/{subject}/test_atomic

/proc/speakto_test(a)
    a << ""Hello World""
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

        Stabalize(g);

        var name = $"/{subject}/test_atomic";
        var instance = g.Create(name);

        Stabalize(g);

        var handle = g.GetHandle(instance);
        g.Environment.Global.ExecProc("speakto_test", handle).CompleteOrException();

        Stabalize(g);

        if (dp.Item2)
            Assert.IsTrue(g.EntityMessages[instance].Single() == "Hello World");
        else
            Assert.IsTrue(g.EntityMessages.Values.Sum(v => v.Count) == 0);
    }
}