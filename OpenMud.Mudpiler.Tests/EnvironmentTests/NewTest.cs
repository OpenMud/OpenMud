using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class NewTest
{
    [Test]
    public void MethodInstantiationTest0()
    {
        var dmlCode =
            @"
mob
    var w = 15
    
    proc
        New(n)
            if(n)
                w += n

proc/test_newop0()
    var/mob/a = new()
    var/mob/b = new/mob()
    var/mob/c = new(1)
    var/mob/d = new/mob(2)

    return a.w + b.w + c.w + d.w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = (int)system.Global.ExecProc("test_newop0").CompleteOrException();
        Assert.IsTrue(r == 15 * 4 + 3);

        Assert.IsTrue(system.Actors.Where(x => x.Type.IsEquivalentTo(system.TypeSolver.Lookup("/mob"))).Count() == 4);
    }


    [Test]
    public void MethodInstantiationTest1()
    {
        var dmlCode =
            @"
mob
    var w = 15
    
    proc
        New(n)
            if(n)
                w += n

proc/test_newop1()
    var c = new/mob(2).w
    var/mob/d = new(3).w
    var e = new /mob/(8)
    var f = new /mob(8)
    var g = new /mob
    
    return c + d + e.w + f.w + g.w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = (int)system.Global.ExecProc("test_newop1").CompleteOrException();
        Assert.IsTrue(r == 17 + 18 + 15 * 2 + 16 + 15);

        Assert.IsTrue(system.Actors.Where(x => x.Type.IsEquivalentTo(system.TypeSolver.Lookup("/mob"))).Count() == 5);
    }

    [Test]
    public void ExampleNew()
    {
        var dmlCode =
            @"
obj/stick
proc/test_newop()
    var/obj/stick/S = new()
    S.desc = ""This is no ordinary stick!""
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        system.Global.ExecProc("test_newop");

        var stick = system.Actors.Where(x => x.Type.IsEquivalentTo(system.TypeSolver.Lookup("/obj/stick"))).Single();

        Assert.IsTrue(stick["desc"] == "This is no ordinary stick!");
    }

    [Test]
    public void ExampleNewWithoutArgList()
    {
        var dmlCode =
            @"
obj/stick
proc/test_newop()
    var/obj/stick/S = new
    S.desc = ""This is no ordinary stick!""
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        system.Global.ExecProc("test_newop");

        var stick = system.Actors.Where(x => x.Type.IsEquivalentTo(system.TypeSolver.Lookup("/obj/stick"))).Single();

        Assert.IsTrue(stick["desc"] == "This is no ordinary stick!");
    }

    [Test]
    public void NewWithEvalType()
    {
        var dmlCode =
            @"
proc/test_newop()
    var T = /list
    var y = new T(10)
    
    if(y.len != 10)
        return 0

    y.Cut()
    y.Add(1)
    y.Add(2)

    if(y[1] + y[2] != 3)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        system.Global.ExecProc("test_newop");

        Assert.IsTrue((int)system.Global.ExecProc("test_newop").CompleteOrException() == 1);
    }

    [Test]
    public void AtomicNewWithEvalSelfType()
    {
        var dmlCode =
            @"
mob
    var w = 0
    New(n)
        if(n)
            w = n
        else
            w = 20
    test_newop()
        var y = new src.type(10)
        return y
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var mob = system.CreateAtomic("/mob");

        dynamic r = mob.ExecProc("test_newop").CompleteOrException();
        Assert.IsTrue(mob != r);

        Assert.IsTrue((int)(r["w"] + mob["w"]) == 30);
    }


    [Test]
    public void DatumNewWithEvalSelfType()
    {
        var dmlCode =
            @"
my_datum
    var q
    New(n)
        if(n)
            q = n
        else
            q = 20
    test_newop()
        var y = new src.type(10)
        return y
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var mob = system.CreateDatum("/my_datum");

        dynamic r = mob.ExecProc("test_newop").CompleteOrException();
        Assert.IsTrue(mob != r);

        Assert.IsTrue((int)(r["q"] + mob["q"]) == 30);
    }
}