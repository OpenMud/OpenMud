using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class IsTypeTestCase
{
    [Test]
    public void SimpleIsTypeImplicitTest()
    {
        var dmlCode =
            @"
/mob/testMob

/proc/test0(var/mob/n)
    return istype(n)

/proc/test1(var/mob/n)
    return istype(n, /mob)

/proc/test2(var/mob/n)
    return istype(n, /mob/testMob)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var testMob = system.CreateAtomic("/mob");
        var testMob2 = system.CreateAtomic("/mob/testMob");

        Assert.IsTrue((bool)system.Global.ExecProc("test0", testMob).CompleteOrException());
        Assert.IsFalse((bool)system.Global.ExecProc("test0", new object[] { null }).CompleteOrException());
        Assert.IsFalse((bool)system.Global.ExecProc("test0", 0).CompleteOrException());

        Assert.IsTrue((bool)system.Global.ExecProc("test1", testMob).CompleteOrException());
        Assert.IsFalse((bool)system.Global.ExecProc("test1", new object[] { null }).CompleteOrException());

        Assert.IsFalse((bool)system.Global.ExecProc("test2", testMob).CompleteOrException());
        Assert.IsFalse((bool)system.Global.ExecProc("test2", new object[] { null }).CompleteOrException());
        Assert.IsTrue((bool)system.Global.ExecProc("test2", testMob2).CompleteOrException());
    }


    [Test]
    public void IsTypeOnPropertyTest()
    {
        var dmlCode = @"

mob/other
    var
        testvar

mob/lerf
    testmethod()
        var/mob/other/t = new()
        t.testvar = 0

        if(istype(t.testvar, /mob/other))
            return 0

        t.testvar = new/mob/other()

        if(!istype(t.testvar, /mob/other))
            return 0

        return 15
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var w = system.CreateAtomic("/mob/lerf");

        var r = w.ExecProc("testmethod").CompleteOrException();

        Assert.IsTrue((int)r == 15);
    }


    [Test]
    public void IsTypeTest()
    {
        var dmlCode =
            @"
/mob
    var n as num
    proc/ExampleIsType0()
        var/mob/q
        q = 0

        if(istype(q, /mob))
            q.n = 5
            world << ""a mob""
        else
            world << ""not a mob""

        q = new/mob
        if(istype(q))
            q.n = 5
            world << ""a mob""
        else
            world << ""not a mob""

    proc/ExampleIsType1()
        var/mob/q
        q = 0
        var namedType = ""/mob""

        if(istype(q, namedType))
            q.n = 5
            world << ""is a mob""
        else
            world << ""not a mob""

        q = new/mob
        if(istype(q, namedType))
            q.n = 5
            world << ""a mob""
        else
            world << ""not a mob""

    proc/ExampleIsType2()
        var/mob/q
        q = 0
        var namedType = /mob

        if(istype(q, namedType))
            q.n = 5
            world << ""is a mob""
        else
            world << ""not a mob""

        q = new/mob
        if(istype(q, namedType))
            q.n = 5
            world << ""a mob""
        else
            world << ""not a mob""

    run_test()
        world << ""Test0""
        ExampleIsType0() //not a mob, a mob
        world << ""Test1""
        ExampleIsType1() //not a mob, not a mob
        world << ""Test2""
        ExampleIsType2() //not a mob, a mob

";
    }
}