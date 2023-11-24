using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class ControlFlowTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestIfStatementControlFlow()
    {
        var dmlCode =
            @"
proc/test_if()
    if(test_input == 0)
        return 5
    else
        return 10

var/num/test_input = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue(10 == (int)system.Global.ExecProc("test_if").CompleteOrException());

        system.Global["test_input"] = 0;

        Assert.IsTrue(5 == (int)system.Global.ExecProc("test_if").CompleteOrException());
    }



    [Test]
    public void TestSingleLineIfStatementControlFlow()
    {
        var dmlCode =
            @"
proc/test_if()
    if(test_input == 0) return 5

    return 10

var/num/test_input = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue(10 == (int)system.Global.ExecProc("test_if").CompleteOrException());

        system.Global["test_input"] = 0;

        Assert.IsTrue(5 == (int)system.Global.ExecProc("test_if").CompleteOrException());
    }

    [Test]
    public void TestForListControlFlowWithTypeFilter()
    {
        var dmlCode =
            @"
/mob/rat
    var w = 5

/proc/test_iter(col)
    var i = 0
    for(var/mob/rat/r in col)
        i += r.w

    return i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var lst = (DatumHandle)system.CreateDatum("/list");

        var custRat = system.CreateAtomic("/mob/rat");
        custRat["w"] = 50;

        lst.ExecProc("Add", system.CreateAtomic("/mob"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", custRat);

        var r = system.Global.ExecProc("test_iter", lst).CompleteOrException();

        Assert.IsTrue((int)r == 65);
    }

    [Test]
    public void TestForListControlFlowWithTypeFilterRecycleVar()
    {
        var dmlCode =
            @"
/mob/rat
    var w = 5

/proc/test_iter(col)
    var i = 0
    var/mob/rat/r
    for(r in col)
        i += r.w

    return i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var lst = (DatumHandle)system.CreateDatum("/list");

        var custRat = system.CreateAtomic("/mob/rat");
        custRat["w"] = 50;

        lst.ExecProc("Add", system.CreateAtomic("/mob"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", custRat);

        var r = system.Global.ExecProc("test_iter", lst).CompleteOrException();

        Assert.IsTrue((int)r == 65);
    }


    [Test]
    public void TestForListControlFlowWithoutTypeFilterRecycleVar()
    {
        var dmlCode =
            @"
/mob/rat
    var w = 5

/proc/test_iter(col)
    var i = 0
    var/r
    for(r in col)
        i += r.w

    return i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var lst = (DatumHandle)system.CreateDatum("/list");

        var custRat = system.CreateAtomic("/mob/rat");
        custRat["w"] = 50;

        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", custRat);

        var r = system.Global.ExecProc("test_iter", lst).CompleteOrException();

        Assert.IsTrue((int)r == 65);
    }


    [Test]
    public void TestForListControlFlowWithTypeFilterNoDecl()
    {
        var dmlCode =
            @"
/mob/rat
    var w = 5

/proc/test_iter(col)
    var i = 0
    var/mob/rat/r
    for(r in col)
        i += r.w

    return i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var lst = (DatumHandle)system.CreateDatum("/list");

        var custRat = system.CreateAtomic("/mob/rat");
        custRat["w"] = 50;

        lst.ExecProc("Add", system.CreateAtomic("/mob"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", system.CreateAtomic("/mob"));
        lst.ExecProc("Add", system.CreateAtomic("/mob/rat"));
        lst.ExecProc("Add", custRat);

        var r = system.Global.ExecProc("test_iter", lst).CompleteOrException();

        Assert.IsTrue((int)r == 65);
    }

    [Test]
    public void TestForListControlFlowWithSelectInstancesOf()
    {
        var dmlCode =
            @"
/mob/rat
    var w = 5

/proc/test_iter()
    var i = 0
    for(var/mob/rat/r)
        i += r.w

    return i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var lst = (DatumHandle)system.CreateDatum("/list");

        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 0);

        var custRat = system.CreateAtomic("/mob/rat");
        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 5);

        custRat["w"] = 50;
        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 50);

        system.CreateAtomic("/mob");
        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 50);

        system.CreateAtomic("/mob/rat");
        system.CreateAtomic("/mob/rat");
        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 60);

        system.CreateAtomic("/mob");
        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 60);

        system.CreateAtomic("/mob/rat");
        Assert.IsTrue((int)system.Global.ExecProc("test_iter").CompleteOrException() == 65);
    }

    [Test]
    public void TestSwitchSimple()
    {
        var dmlCode =
            @"
/proc/testproc(a0)
    var retVal = 0
    switch(a0)
        if(1)          retVal = 1
        if(2)          retVal = 2
        if(1|2|4)       retVal = 7
        if(15 to 30)   retVal = 20
        if(50 to 40)   retVal = 30
        if(55, 58, 70) retVal = 40
        if(1 to 2)     retVal = 2222
        else           retVal = 1111

    return retVal
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("testproc", 1).CompleteOrException() == 1);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 2).CompleteOrException() == 2);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 1 | 2 | 4).CompleteOrException() == 7);


        Assert.IsTrue((int)system.Global.ExecProc("testproc", 15).CompleteOrException() == 20);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 16).CompleteOrException() == 20);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 30).CompleteOrException() == 20);

        Assert.IsTrue((int)system.Global.ExecProc("testproc", 50).CompleteOrException() == 30);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 45).CompleteOrException() == 30);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 40).CompleteOrException() == 30);


        Assert.IsTrue((int)system.Global.ExecProc("testproc", 55).CompleteOrException() == 40);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 58).CompleteOrException() == 40);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 70).CompleteOrException() == 40);

        Assert.IsTrue((int)system.Global.ExecProc("testproc", 54).CompleteOrException() == 1111);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 56).CompleteOrException() == 1111);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 57).CompleteOrException() == 1111);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 68).CompleteOrException() == 1111);
        Assert.IsTrue((int)system.Global.ExecProc("testproc", 100).CompleteOrException() == 1111);
    }

    [Test]
    public void TestForLoopContinueStatement()
    {
        var dmlCode =
            @"
/proc/test_iter()
    var testset = list(5,8,6,7,9,2,1,3)
    var total = 0
    for(var/r in testset)
        if(r <= 6)
            continue
        
        total += r

    return total
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_iter").CompleteOrException();
        Assert.IsTrue(r == 24);
    }

    [Test]
    public void TestForLoopBreakStatement()
    {
        var dmlCode =
            @"
/proc/test_iter()
    var testset = list(5,8,6,7,9,2,1,3)
    var total = 0
    for(var/r in testset)
        total += r
        if(r == 2)
            break

    return total
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_iter").CompleteOrException();
        Assert.IsTrue(r == 37);
    }



    [Test]
    public void TestWhileLoop()
    {
        var dmlCode =
            @"
/proc/test_iter()
    var testset = list(5,8,6,7,9,2,1,3)
    var total = 0
    var/i = 1
    while(i <= testset.len)        
        total += testset[i]
        i++

    return total
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_iter").CompleteOrException();
        Assert.IsTrue(r == new[] { 5, 8, 6, 7, 9, 2, 1, 3 }.Sum());
    }

    [Test]
    public void TestWhileLoopContinueStatement()
    {
        var dmlCode =
            @"
/proc/test_iter()
    var testset = list(5,8,6,7,9,2,1,3)
    var total = 0
    for(var/r in testset)
        if(r <= 6)
            continue
        
        total += r

    return total
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_iter").CompleteOrException();
        Assert.IsTrue(r == 24);
    }

    [Test]
    public void TestWhileLoopBreakStatement()
    {
        var dmlCode =
            @"
/proc/test_iter()
    var testset = list(5,8,6,7,9,2,1,3)
    var total = 0
    var/i = 1
    while(i <= testset.len)
        if(i >= 6)
            i++
            break
        
        total += testset[i]
        i++

    return total
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_iter").CompleteOrException();
        Assert.IsTrue(r == new[] { 5, 8, 6, 7, 9 }.Sum());
    }

    [Test]
    public void TestIfSingleReturnStatementOneline()
    {
        var dmlCode =
            @"
/proc/test()
    var w = 8

    if(w == 8) return 1

    return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1);
    }

    [Test]
    public void TestIfSingleReturnStatementOnelineWithNoValue()
    {
        var dmlCode =
            @"
/proc/test()
    var w = 8
    . = 1
    if(w == 8) return

    . = 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1);
    }

    [Test]
    public void ForLoopRecycleVarTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i
    for(i=1, i<=3, i++)
        w *= (i + 1)

    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4);
    }

    [Test]
    public void ForLoopRecycleVarIndexerTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i
    for(i=1, i<=3, i++)
        w *= (i + 1)

    return i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 4);
    }

    [Test]
    public void ForLoopRecycleVarInRangeTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i
    for(i in 1 to 3)
        w *= (i + 1)
    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();

        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 3);
    }


    [Test]
    public void ForLoopRecycleVarInRangeEarlyReturnTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i
    for(i in 1 to 3)
        w *= (i + 1)
        return w
    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 2);
    }


    [Test]
    public void ForLoopVarInRangeTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    for(var/i in 1 to 3)
        w *= (i + 1)

    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4);
    }

    [Test]
    public void ForLoopRecycleNoDecl()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1
    for(, i<=3, i++)
        w *= (i + 1)

    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 4);
    }

    [Test]
    public void ForLoopRecycleNoDeclNoTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1
    for(,, i++)
        if(i > 3)
            break
        w *= (i + 1)

    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 4);
    }

    [Test]
    public void ForLoopRecycleNoDeclNoTestNoStep()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1
    for(,,)
        if(i > 3)
            break
        w *= (i + 1)
        i++

    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 4);
    }

    [Test]
    public void ForLoopDeclTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1

    for(var/i=1, i<=3, i++)
        w *= (1 + i)

    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4);
    }

    [Test]
    public void ForLoopDeclWithNopInitTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1
    for(i, i<=3, i++)
        w *= (1 + i)

    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4);
    }

    [Test]
    public void ForLoopDeclInRangeTest()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1

    for(var/i in 1 to 3)
        w *= (1 + i)

    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4);
    }



    [Test]
    public void ForLoopContinueLabel()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1

    outer:
        for(var/i in 1 to 3)
            w *= (1 + i)
            var/x = 0
            for(x in 8 to 15)
                if(x == 11)
                    continue outer
                w += x
    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();

        Assert.IsTrue(r == 483);
    }

    [Test]
    public void ForLoopBreakLabel()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1

    outer:
        for(var/i in 1 to 3)
            w *= (1 + i)
            var/x = 0
            for(x in 8 to 15)
                if(x == 11)
                    break outer
                w += x
    return w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        //Should be 29
        Assert.IsTrue(r == 29);
    }

    [Test]
    public void LabelGotoTest0()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1

    start_loop

    w *= (1 + i)
    i += 1
    if(i <= 3)
        goto start_loop

    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 4);
    }

    [Test]
    public void LabelGotoTest1()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1

    start_loop
        w *= (1 + i)
        i += 1
        if(i <= 3)
            goto start_loop

    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 4);
    }


    [Test]
    public void LabelGotoTest3()
    {
        var dmlCode =
            @"
/proc/test()
    var/w = 1
    var/i = 1

    start_loop:
        w *= (1 + i)
        i += 1
        if(i <= 3)
            goto start_loop

    return w + i
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(r == 1 * 2 * 3 * 4 + 4);
    }

    [Test]
    public void LabelGotoTest2()
    {
        var dmlCode =
            @"
/proc/test()
    emptylabel
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = system.Global.ExecProc("test").CompleteOrException();
    }
}