using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class PropertyAccessors
{
    [Test]
    public void SelfPropertyAccessor()
    {
        var dmlCode =
            @"
mob
    var w = 0

/mob/test0()
    var n = 2 * (src.w + 5)
    return n
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mobTest = system.CreateAtomic("/mob");
        mobTest["w"] = 25;

        Assert.IsTrue((int)mobTest.ExecProc("test0").CompleteOrException() == 60);
    }

    [Test]
    public void SelfPropertyAssignment()
    {
        var dmlCode =
            @"
mob
    var w = 0

/mob/test0()
    src.w = 30

/mob/test1()
    return src.w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mobTest = system.CreateAtomic("/mob");
        mobTest["w"] = 8;

        Assert.IsTrue(mobTest["w"] == 8);
        mobTest.ExecProc("test0");
        Assert.IsTrue(mobTest["w"] == 30);

        Assert.IsTrue((int)mobTest.ExecProc("test1").CompleteOrException() == 30);
    }

    [Test]
    public void LaxPropertyAssignment()
    {
        var dmlCode =
            @"
mob
    var w = 0

/mob/test0()
    src:w = 30

/mob/test1()
    return src:w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mobTest = system.CreateAtomic("/mob");
        mobTest["w"] = 8;

        Assert.IsTrue(mobTest["w"] == 8);
        mobTest.ExecProc("test0");
        Assert.IsTrue(mobTest["w"] == 30);

        Assert.IsTrue((int)mobTest.ExecProc("test1").CompleteOrException() == 30);
    }

    [Test]
    public void MethodReturnGetter()
    {
        var dmlCode =
            @"
mob
    var w = 0

/mob/test()
    return src

/proc/test1(var/mob/n)
    return n.test().w + 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mobTest = system.CreateAtomic("/mob");
        mobTest["w"] = 25;

        Assert.IsTrue((int)system.Global.ExecProc("test1", mobTest).CompleteOrException() == 35);
    }
}