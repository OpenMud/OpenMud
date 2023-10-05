using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

//Whoever wrote Dream Maker Language could take a REALLY VALUABLE lesson in minimizing COMPLEXITY
//The number of dimensions to every single little feature of DML is absurd.
public class FlexibleArguments
{
    [Test]
    public void RedeclareMoreArgumentsAndCallBase()
    {
    }

    [Test]
    public void RedeclareMoreLessAndCallBase()
    {
    }

    [Test]
    public void BaseInheritesArgumentListWhenEmpty()
    {
        var dmlCode =
            @"
/mob
    proc
        test(a as num)
            return a * a

/mob/sub
    proc
        test(a as num)
            return a + ..()
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test", 5).CompleteOrException() == 30);
    }

    [Test]
    public void SelfInheritesArgumentListWhenEmpty()
    {
        var dmlCode =
            @"
/mob
    var/num/testv
    testv = 5
    proc
        test(a as num)
            testv = testv - 1
            if (testv == 0)
                return 0

            return a + .()
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        Assert.IsTrue((int)mob.ExecProc("test", 5).CompleteOrException() == 20);
    }

    [Test]
    public void SelfDoesntInheritesArgumentListWhenNotEmpty()
    {
        var dmlCode =
            @"

/mob
    var/num/testv
    testv = 5
    proc
        test(a as num)
            testv = testv - 1
            if (testv == 0)
                return 0

            return a + .(a - 1)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        Assert.IsTrue((int)mob.ExecProc("test", 5).CompleteOrException() == 5 + 4 + 3 + 2);
    }

    [Test]
    public void BaseDoesntInheritesArgumentListWhenNotEmpty()
    {
        var dmlCode =
            @"
/mob
    proc
        test(a as num)
            return a * a

/mob/sub
    proc
        test(a as num)
            return a + ..(a - 1)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test", 5).CompleteOrException() == 21);
    }

    [Test]
    public void ExcessArgumentsAreIgnoredWithoutProblems()
    {
        var dmlCode =
            @"
/mob
    proc
        test(a as num)
            return a * a

/mob/sub
    proc
        test(a as num)
            return a + ..(a - 1)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test", 5, 6, 7, 8).CompleteOrException() == 21);
    }

    [Test]
    public void ExcessArgumentsInherited()
    {
        var dmlCode =
            @"
/mob
    proc
        test(a as num, b as num, c as num)
            return a * (b + c)

/mob/sub
    proc
        test(a as num)
            return a + ..()
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test", 5, 6, 7, 8, 9).CompleteOrException() == 5 + 5 * (6 + 7));
    }


    [Test]
    public void MissingArgumentsAreAssignedDefaults()
    {
        var dmlCode =
            @"
/mob/sub
    proc
        test(a as num, b as num)
            return a + b + 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test").CompleteOrException() == 10);
    }

    [Test]
    public void InvokeWithSrcAsArgument()
    {
        var dmlCode =
            @"
/mob
    var w = 20
    proc
        test2(a)
            return a.w
        test()
            return test2(src)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        Assert.IsTrue((int)mob.ExecProc("test").CompleteOrException() == 20);
    }

    [Test]
    public void NamedArgumentsTest()
    {
        var dmlCode =
            @"
/mob
    proc
        test2(var/a, var/b)
            return (a + 2) * (b - 3)
        test()
            return test2(b=7, a = 15)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");

        var r = (int)mob.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 17 * 4);
    }
}