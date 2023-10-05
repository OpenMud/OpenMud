using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.Tests.EcsTest;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class TasksTests
{
    private static void Stabalize(TestGame w, float seconds)
    {
        for (var i = 0; i < 100; i++)
            w.Update(seconds / 100.0f);
    }

    [Test]
    public void TestSleepManual()
    {
        var dmlCode =
            @"
/proc/task_test()
    var w = 1
    world << ""Stage 1""
    sleep(10)
    w *= 2
    world << ""Stage 2""
    sleep(10)
    w *= 2
    world << ""Stage 3""
    sleep(10)
    w *= 2
    world << ""Stage 4""
    sleep(10)
    w *= 2
    world << ""Stage 5""
    sleep(10)
    w *= 2
    world << ""Stage 6""
    sleep(10)
    w *= 2
    return w
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

        Stabalize(g, 1);

        var interruptCount = 0;

        var ctx = g.Environment.Global.ExecProc("task_test", new object[0], false);
        while (true)
        {
            try
            {
                ctx.UnmanagedContinue();
            }
            catch (DeferExecutionException e)
            {
                interruptCount++;
                Assert.IsTrue(e.LengthMilliseconds == 1000);

                var expectedStages = Enumerable.Range(0, interruptCount).Select(i => $"Stage {i + 1}").ToList();

                Assert.IsTrue(
                    g.WorldMessages.ToList()
                        .SequenceEqual(expectedStages)
                );

                continue;
            }

            break;
        }

        Assert.IsTrue(interruptCount == 6);
        Assert.IsTrue((int)ctx.Result == (int)Math.Pow(2, 6));
    }

    [Test]
    public void TestTasksManaged()
    {
        var dmlCode =
            @"
/proc/task_test()
    var w = 1
    world << ""Stage 1""
    sleep(10)
    w *= 2
    world << ""Stage 2""
    sleep(10)
    w *= 2
    world << ""Stage 3""
    sleep(10)
    w *= 2
    world << ""Stage 4""
    sleep(10)
    w *= 2
    world << ""Stage 5""
    sleep(10)
    w *= 2
    world << ""Stage 6""
    sleep(10)
    w *= 2
    return w
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

        var ctx = g.Environment.Global.ExecProc("task_test");

        Stabalize(g, 0.5f);

        for (var i = 1; i <= 6; i++)
        {
            var expectedStages = Enumerable.Range(0, i).Select(i => $"Stage {i + 1}").ToList();

            Assert.IsTrue(
                g.WorldMessages.ToList()
                    .SequenceEqual(expectedStages)
            );

            Stabalize(g, 1.0f);
        }

        Assert.IsTrue(ctx.State == DatumProcExecutionState.Completed);
        Assert.IsTrue((int)ctx.Result == (int)Math.Pow(2, 6));
    }


    [Test]
    public void SpawnStatementTest()
    {
        var dmlCode =
            @"
/proc/task_test()
    spawn(10) world << ""Hello from Spawn!""
    world << ""Hello from proc.""
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

        var ctx = g.Environment.Global.ExecProc("task_test");

        Stabalize(g, 0.5f);

        Assert.IsTrue(
            g.WorldMessages.ToList()
                .SequenceEqual(new[]
                {
                    "Hello from proc."
                })
        );

        Stabalize(g, 1.0f);

        Assert.IsTrue(
            g.WorldMessages.ToList()
                .SequenceEqual(new[]
                {
                    "Hello from proc.",
                    "Hello from Spawn!"
                })
        );
    }


    [Test]
    public void SpawnBlockTest()
    {
        var dmlCode =
            @"
/proc/task_test()
    spawn(10)
        world << ""Hello from Spawn!""
        world << ""Hello again from Spawn!""

    world << ""Hello from proc.""
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

        var ctx = g.Environment.Global.ExecProc("task_test");

        Stabalize(g, 0.5f);

        Assert.IsTrue(
            g.WorldMessages.ToList()
                .SequenceEqual(new[]
                {
                    "Hello from proc."
                })
        );

        Stabalize(g, 1.0f);

        Assert.IsTrue(
            g.WorldMessages.ToList()
                .SequenceEqual(new[]
                {
                    "Hello from proc.",
                    "Hello from Spawn!",
                    "Hello again from Spawn!"
                })
        );
    }


    [Test]
    public void SpawnStateIsolationTest()
    {
        var dmlCode =
            @"
/proc/task_test()
    var x = 10
    spawn(10)
        world << x
        x -= 1
        world << x
    x += 1
    world << x
";
        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));
        var g = new TestGame(new NullSceneBuilder(5, 5), assembly);

        var ctx = g.Environment.Global.ExecProc("task_test");

        Stabalize(g, 0.5f);

        Assert.IsTrue(
            g.WorldMessages.ToList()
                .SequenceEqual(new[]
                {
                    "11"
                })
        );

        Stabalize(g, 1.0f);

        Assert.IsTrue(
            g.WorldMessages.ToList()
                .SequenceEqual(new[]
                {
                    "11",
                    "10",
                    "9"
                })
        );
    }
}