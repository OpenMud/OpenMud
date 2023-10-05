using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class TypesOfTest
{
    [Test]
    public void SimpleTypesOfTest()
    {
        var dmlCode =
            @"
/mob/t0/a0
/mob/t0/b0
/mob/t1/a1
/mob/t1/b1

/obj/t0/c0
/obj/t0/d0
/obj/t1/c1


/proc/test0()
    return typesof(/mob)

/proc/test1()
    return typesof(/obj)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        dynamic r = system.Global.ExecProc("test0").CompleteOrException();

        Assert.IsTrue((int)r["len"] == 6);

        r = system.Global.ExecProc("test1").CompleteOrException();
        Assert.IsTrue((int)r["len"] == 5);
    }

    [Test]
    public void SimpleTypesOfAndInstantiateTest()
    {
        var dmlCode =
            @"
/mob/t0
/mob/t0
/mob/t0/a0
/mob/t0/b0
/mob/t1/a1
/mob/t1/b1


/proc/test0()
    var allTypes = typesof(/mob)

    if(allTypes.len != 6)
        return 0

    var t = list(new allTypes[1], new allTypes[2], new allTypes[3], new allTypes[4], new allTypes[5], new allTypes[6])

    if(t.len != 6 || !t[1])
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        var r = system.Global.ExecProc("test0").CompleteOrException();

        Assert.IsTrue((int)r == 1);
    }
}