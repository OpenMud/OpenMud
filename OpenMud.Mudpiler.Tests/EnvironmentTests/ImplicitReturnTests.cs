using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

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

    [Test]
    public void ManipulateImplicitReturn()
    {
        var dmlCode =
            @"

/proc/test_uniquelist()
    var l = list(1,1,1,4,5,6,4,6,5,4)
    return uniquelist(l)

/proc/uniquelist(var/list/L)
    . = list()
    for(var/item in L)
        if(!(item in .))
            . += item
";

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = (DatumHandle)system.Global.ExecProc("test_uniquelist").CompleteOrException();

        var l = r.Unwrap<DmlList>().Host.Select(e => DmlEnv.AsNumeric(e.Key)).ToList();

        Assert.IsTrue(l.SequenceEqual(new[]
        {
            1,4,5,6
        }));
    }

    [Test]
    public void ArrayIndexImplicitReturn()
    {
        var dmlCode =
            @"
/proc/testIndex()
    . = list(5,6,7,1)

    return .[2]
";

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = system.Global.ExecProc("testIndex").CompleteOrException();

        Assert.IsTrue(DmlEnv.AsNumeric(r) == 6);
    }
}