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

        //Because we are directly executing the speakto_test on the logic environment, it should propogate immediately. But for good practice we will give the ECS
        //time to stabalize
        Stabalize(g);

        if (dp.Item2)
            Assert.IsTrue(g.EntityMessages[instance].Single() == "Hello World");
        else
            Assert.IsTrue(g.EntityMessages.Values.Sum(v => v.Count) == 0);
    }


    /*
    [Test]
    public void ListWriteBroadcastTest()
    {

        var dmlCode =
@"

/mob/bob

/mob/tod

/datum/watum

/proc/test0()
    var/list/a = newlist(/mob/tod, /mob/bob, /datum/watum)

    a << ""Hello World?""

    return 1
";
        var assembly = SimpleDmlCompiler.CompileAndLoad(dmlCode);
        var env = new TestPhysicalEnvironmentSolver();
        var system = ByondEnvironment.CreateFromAssembly(Assembly.LoadFile(assembly), new BaseDmlFramework(env));

        system.Global.ExecProc("test0");

        var echos = env.Echos.ToDictionary(x => x.Key, x => x.Value.Single());

        Assert.IsTrue(echos.Values.Count == 2 && echos.Values.All(x => x == "Hello World?"));
    }
    */
}