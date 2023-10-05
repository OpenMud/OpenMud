using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class InheritanceTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void FieldInitializationOrder()
    {
        var dmlCode =
            @"
turf
    var tx as num
    tx = 1
    trap
        tx = 2
        pit
            tx = 3
        quicksand
            tx = 4
        glue
            tx = 5
";
        var tests = new List<(string obj, int val)>
        {
            ("/turf", 1),
            ("/turf/trap", 2),
            ("/turf/trap/pit", 3),
            ("/turf/trap/quicksand", 4),
            ("/turf/trap/glue", 5)
        };


        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        foreach (var (obj, val) in tests)
        {
            var t = system.CreateAtomic(obj);
            Assert.True(val == (int)t["tx"]);
        }
    }


    [Test]
    public void FieldOverlappingClassNames()
    {
        var dmlCode =
            @"
turf
    var tx as num
    tx = 1
    trap
        tx = 2
        trap
            tx = 3
        trapa
            tx = 4
        trapb
            tx = 5
";
        var tests = new List<(string obj, int val)>
        {
            ("/turf", 1),
            ("/turf/trap", 2),
            ("/turf/trap/trap", 3),
            ("/turf/trap/trapa", 4),
            ("/turf/trap/trapb", 5)
        };


        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        foreach (var (obj, val) in tests)
        {
            var t = system.CreateAtomic(obj);
            Assert.True(val == (int)t["tx"]);
        }
    }


    [Test]
    public void SelfCalls()
    {
        var dmlCode =
            @"
proc/fib(a as num)
    if(a <= 1)
        return a
    
    return .(a - 1) + .(a-2)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("fib", 10).CompleteOrException() == 55);
    }


    [Test]
    public void BaseCalls()
    {
        var dmlCode =
            @"
turf
    verb/test(a as num)
        return a * a

turf/turfb/test(a as num)
    return ..(a + 1) + a
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var turfsub = system.CreateAtomic("/turf/turfb");
        var turfbase = system.CreateAtomic("/turf");
        //turf.Proc("testa", 10)
        Assert.IsTrue(11 * 11 + 10 == (int)turfsub.ExecProc("test", 10).CompleteOrException());
        Assert.IsTrue(5 * 5 == (int)turfbase.ExecProc("test", 5).CompleteOrException());
    }


    [Test]
    public void ReDeclarationOverrides()
    {
        var dmlCode =
            @"
turf
    test(a as num)
        return a * a

turf/test(a as num)
    return ..(a + 1) + a
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var turfsub = system.CreateAtomic("/turf");

        var r = (int)turfsub.ExecProc("test", 10).CompleteOrException();

        Assert.IsTrue(11 * 11 + 10 == r);
    }


    [Test]
    public void TestInheritName()
    {
        var dmlCode =
            @"
turf/door
    name = ""Door""

turf/door/open
    verb
        close()
            return

turf/door/closed
    verb
        open()
            return
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var door = system.CreateAtomic("/turf/door");
        var doorOpen = system.CreateAtomic("/turf/door/open");
        var doorClosed = system.CreateAtomic("/turf/door/closed");

        Assert.IsTrue(door["name"] == "Door");
        Assert.IsTrue(doorOpen["name"] == "Door");
        Assert.IsTrue(doorClosed["name"] == "Door");
    }
}