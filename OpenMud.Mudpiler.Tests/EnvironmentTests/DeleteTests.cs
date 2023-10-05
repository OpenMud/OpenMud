using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class DeleteTests
{
    [Test]
    public void TestDeleteViaEntityHandle()
    {
        var dmlCode =
            @"
mob
    var w = 15

var/globaltest

proc/test_create()
    globaltest = new/mob()

proc/test_use()
    if(globaltest)
        return globaltest.w + 5
    else
        return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 0);

        system.Global.ExecProc("test_create");
        r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 20);
        r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 20);

        var eh = (DatumHandle)system.Global["globaltest"];
        eh.Destroy();

        r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 0);
    }

    [Test]
    public void TestDeleteWithOperator()
    {
        var dmlCode =
            @"
mob
    var w = 15

var/globaltest

proc/test_create()
    globaltest = new/mob()


proc/test_delete()
    del globaltest

proc/test_use()
    if(globaltest)
        return globaltest.w + 5
    else
        return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 0);

        system.Global.ExecProc("test_create");
        r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 20);
        r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 20);

        system.Global.ExecProc("test_delete");

        r = (int)system.Global.ExecProc("test_use").CompleteOrException();
        Assert.IsTrue(r == 0);
    }


    [Test]
    public void TestInSubroutineWithOperator()
    {
        var dmlCode =
            @"
proc/test_use()
    var/mob/test = new()
    var y = test

    if(!test || !y)
        return 0

    del test
    
    if(y)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test_use").CompleteOrException() == 1);
    }

    [Test]
    public void DeleteRemovesFromWorldPieces()
    {
        var dmlCode =
            @"
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var baseCount = system.Actors.Count();

        var mob = system.CreateAtomic("/mob");

        Assert.IsTrue(system.Actors.Contains(mob));

        mob.Destroy();

        Assert.IsTrue(!system.Actors.Contains(mob));
    }
}