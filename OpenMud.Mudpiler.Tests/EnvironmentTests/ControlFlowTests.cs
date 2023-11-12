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
}