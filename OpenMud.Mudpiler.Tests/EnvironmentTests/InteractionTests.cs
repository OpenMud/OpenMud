using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class InteractionTests
{
    [Test]
    public void PropertyGetterSetter()
    {
        var dmlCode =
            @"
/proc/testattr(a)
    a.health = a.health - 10

/mob
    var health as num
    health = 100
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        system.Global.ExecProc("testattr", mob);
        system.Global.ExecProc("testattr", mob);
        system.Global.ExecProc("testattr", mob);
        Assert.IsTrue(70 == (int)mob["health"]);
    }

    [Test]
    public void UsrSrcContextTest()
    {
        var dmlCode =
            @"
obj/dumbbell
    var weight as num

    verb
        pickup()
            usr.weight = usr.weight + src.weight
        drop()
            usr.weight = usr.weight - src.weight

/obj/dumbbell/lb10
    weight=10

/obj/dumbbell/lb15
    weight=15

/obj/dumbbell/lb25
    weight=25

/mob
    var weight as num
    weight = 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var dumbbell_10lb = system.CreateAtomic("/obj/dumbbell/lb10");
        var dumbbell_15lb = system.CreateAtomic("/obj/dumbbell/lb15");
        var dumbbell_25lb = system.CreateAtomic("/obj/dumbbell/lb25");

        var playera = system.CreateAtomic("/mob");
        var playerb = system.CreateAtomic("/mob");

        Assert.IsTrue(0 == (int)playera["weight"]);
        Assert.IsTrue(0 == (int)playerb["weight"]);

        playera.Interact(dumbbell_10lb, "pickup");
        playera.Interact(dumbbell_25lb, "pickup");

        playerb.Interact(dumbbell_15lb, "pickup");

        Assert.IsTrue(35 == (int)playera["weight"]);
        Assert.IsTrue(15 == (int)playerb["weight"]);

        playera.Interact(dumbbell_10lb, "drop");
        Assert.IsTrue(25 == (int)playera["weight"]);

        playera.Interact(dumbbell_25lb, "drop");
        Assert.IsTrue(0 == (int)playera["weight"]);

        playerb.Interact(dumbbell_15lb, "drop");
        Assert.IsTrue(0 == (int)playerb["weight"]);
    }
}