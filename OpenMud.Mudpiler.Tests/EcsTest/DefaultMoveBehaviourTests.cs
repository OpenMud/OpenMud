using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EcsTest;

public class DefaultMoveBehaviourTests
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }

    [Test]
    public void TestObjMove_AddsToContents()
    {
        var dmlCode =
            @"
mob
    testmob
        New()
            x = 1
            y = 1
            layer = 1

obj
    testobj
        New()
            x = 2
            y = 2
            layer = 1
        verb
            test_move()
                set src in oview(100)
                Move(usr)
";

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(10, 10), assembly);

        Stabalize(g);
        var testmob = g.Create("/mob/testmob");
        var testobj = g.Create("/obj/testobj");
        Stabalize(g);

        g.ExecuteVerb(testmob, testobj, "test_move", new string[] { });
        Stabalize(g);

        var testMobHandle = g.GetHandle(testmob);
        var testObjHandle = g.GetHandle(testobj);

        Assert.IsTrue((bool)((DatumHandle)testMobHandle["contents"]).ExecProc("Contains", testObjHandle)
            .CompleteOrException());

        Assert.IsTrue((int)testMobHandle["x"] == 1);
        Assert.IsTrue((int)testMobHandle["y"] == 1);
        Assert.IsTrue((int)testMobHandle["layer"] == 1);

        Assert.IsTrue((int)testObjHandle["x"] == 2);
        Assert.IsTrue((int)testObjHandle["y"] == 2);
        Assert.IsTrue((int)testObjHandle["layer"] == 1);
    }


    [Test]
    public void TestMobMove_Jump()
    {
        var dmlCode =
            @"
mob
    testmob

        New()
            x = 1
            y = 1
            layer = 3
        
        verb
            test_move()
                return Move(locate(/turf/testturf))

turf
    testturf
        New()
            x = 5
            y = 6
            layer = 2
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(10, 10), assembly);

        Stabalize(g);
        var testmob = g.Create("/mob/testmob");
        g.Create("/turf/testturf");
        Stabalize(g);

        g.ExecuteVerb(testmob, testmob, "test_move", new string[] { });
        Stabalize(g);

        var testMobHandle = g.GetHandle(testmob);

        Assert.IsTrue((int)testMobHandle["x"] == 5);
        Assert.IsTrue((int)testMobHandle["y"] == 6);
        Assert.IsTrue((int)testMobHandle["layer"] == 3);
    }

    [Test]
    public void TestMobMove_Slide()
    {
        var dmlCode =
            @"
mob
    testmob

        New()
            x = 1
            y = 1
            layer = 3
        
        verb
            test_move()
                return Move(locate(/turf/testturf))

turf
    testturf
        New()
            x = 2
            y = 1
            layer = 3
        
        Enter()
            world << ""slide""
            return 1
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(10, 10), assembly);

        Stabalize(g);
        var testmob = g.Create("/mob/testmob");
        g.Create("/turf/testturf");
        Stabalize(g);

        g.ExecuteVerb(testmob, testmob, "test_move", new string[] { });
        Stabalize(g);

        var testMobHandle = g.GetHandle(testmob);

        Assert.IsTrue((int)testMobHandle["x"] == 2);
        Assert.IsTrue((int)testMobHandle["y"] == 1);
        Assert.IsTrue((int)testMobHandle["layer"] == 3);

        Assert.IsTrue(g.WorldMessages.Single() == "slide");
    }
}