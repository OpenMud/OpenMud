using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

internal class ImplicitReturnTests
{
    [Test]
    public void ExplicitReturn()
    {
        var dmlCode =
            @"
/mob/sub
    proc
        test()
            return 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test").CompleteOrException() == 10);
    }

    [Test]
    public void ImplicitReturn()
    {
        var dmlCode =
            @"
/mob/sub
    var/num/x
    proc
        test()
            . = 15
            x = 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test").CompleteOrException() == 15);
    }

    [Test]
    public void ImplicitReturnWithBlankReturn()
    {
        var dmlCode =
            @"
/mob/sub
    proc
        test(a as num)
            . = 15
            if(a == 1)
                return

            return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test", 1).CompleteOrException() == 15);
        Assert.IsTrue((int)mob.ExecProc("test", 0).CompleteOrException() == 1);
    }

    [Test]
    public void ExplicitReturnWithImplicitReturn()
    {
        var dmlCode =
            @"
/mob/sub
    proc
        test()
            . = 15
            return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/sub");
        Assert.IsTrue((int)mob.ExecProc("test").CompleteOrException() == 1);
    }
}