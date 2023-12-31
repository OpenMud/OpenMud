﻿using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
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

    [Test]
    public void DefaultParameterValueTest()
    {
        var dmlCode =
            @"
/mob
    proc
        test2(var/a=9, var/b)
            return (a + 2) * (b - 3)
        test()
            return test2(b=7)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");

        var interim = mob.ExecProc("test").CompleteOrException();
        var r = (int)interim;
        Assert.IsTrue(r == 44);
    }

    [Test]
    public void ComplexDefaultArguments()
    {
        var dmlCode =
            @"
/proc/wow(var/x)
    return x * 10

/mob
    proc
        test2(var/a=wow(9), var/b)
            return (a + 2) * (b - 3)
        test()
            return test2(b=7)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");

        var interim = mob.ExecProc("test").CompleteOrException();
        var r = (int)interim;
        Assert.IsTrue(r == (90 + 2) * (7 - 3));
    }

    [Test]
    public void ComplexDefaultArguments2()
    {
        var dmlCode =
            @"
/proc/wow(var/x)
    return x * 10

/mob
    proc
        test2(var/a=wow(9) - 4, var/b)
            return (a + 2) * (b - 3)
        test()
            return test2(b=7)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");

        var interim = mob.ExecProc("test").CompleteOrException();
        var r = (int)interim;
        Assert.IsTrue(r == ((90 - 4) + 2) * (7 - 3));
    }

    [Test]
    public void OmitPositionalArgumentsWithBlank()
    {
        var dmlCode =
            @"
/proc/wow(var/a, var/b, var/c)
    var d = b
    if(!d)
        d = -2

    return a * d * c

/proc/test0()
    return wow(4,2,3)

/proc/test1()
    return wow(4,,3)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var t0 = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue(t0 == 4 * 2 * 3);

        var t1 = (int)system.Global.ExecProc("test1").CompleteOrException();
        Assert.IsTrue(t1 == 4 * -2 * 3);
    }

    [Test]
    public void DefineNamedArgumentWithExprTest0()
    {
        var dmlCode =
            @"
/proc/tgt(var/c, var/d)
    return c - d

/proc/test0()
    return tgt(""d""=10, ""c""=4)

";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var t0 = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue(t0 == 4 - 10);
    }

    [Test]
    public void DefineNamedArgumentWithExprTest1()
    {
        var testCode =
            @"
/proc/tgt(var/c, var/d)
    return c - d

/proc/test0()
    var/t0 = ""d""
    var/t1 = ""c""
    return tgt(""[t0]""=10, ""[t1]""=4)

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var t0 = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue(t0 == 4 - 10);
    }

    [Test]
    public void AtomParameterWithoutVarWithRootedType()
    {
        var testCode =
            @"
/proc/tgt(/atom/t)
    if(istype(t))
        return 1000

    return 11

/proc/test0()
    var/w = tgt(0)
    w += tgt(new/atom())
    return w

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var t0 = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue(t0 == 1000 + 11);
    }

    [Test]
    public void AtomParameterWithoutVarWithUnrootedType()
    {
        var testCode =
            @"
/proc/tgt(atom/t)
    if(istype(t))
        return 1000

    return 11

/proc/test0()
    var/w = tgt(0)
    w += tgt(new/atom())
    return w

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var t0 = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue(t0 == 1000 + 11);
    }

    [Test]
    public void DiscardArgumentsTest()
    {
        var testCode =
            @"
/proc/tgt(null, var/d)
    return d * d

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var t0 = (int)system.Global.ExecProc("tgt", 5, 8).CompleteOrException();
        Assert.IsTrue(t0 == 8*8);
    }
}