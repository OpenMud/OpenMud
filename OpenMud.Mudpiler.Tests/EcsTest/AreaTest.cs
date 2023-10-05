using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Tests.EcsTest;

internal class AreaTest
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }


    [Test]
    public void EnterExitTest()
    {
        var dmlCode =
            @"
/mob/player
    density = 1.0

    New()
        Move(locate(/area/entry))
        world << ""spawn""

    Bump(mob/other)
        world << ""Bumped""

/turf/wall
    density = 1.0

/area/entry

/area/a0
    var vv = 0
    Enter(t)
        vv += 1
        world << vv
        return 1

/area/a1
    var vv = 10
    Enter(t)
        vv += 1
        world << vv
        return 1


/turf/floor
";

        var dmmCode =
            @"
""x"" = (/turf/wall)
""a"" = (/turf/floor,/area/a0)
""b"" = (/turf/floor,/area/a1)
""e"" = (/turf/floor,/area/entry)

(1,1,1) = {""
xxx
xex
xax
xax
xbx
xbx
xax
xax
xbx
xbx
xxx
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var withPlayerScene = new SimpleSceneBuilder(sceneBuilder.Bounds, w =>
        {
            sceneBuilder.Build(w);
            var player = w.CreateEntity();
            entityBuilder.CreateAtomic(player, "/mob/player", "player");
            player.Set<PlayerCanImpersonateComponent>();
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(withPlayerScene, assembly);

        Stabalize(g);

        for (var i = 0; i < 8; i++)
        {
            g.Slide("player", 0, 1);
            Stabalize(g);
        }

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "spawn",
            "1",
            "11",
            "2",
            "12"
        }));
    }


    [Test]
    public void NewTurfReplacesExistingTest()
    {
        var dmlCode =
            @"
/mob/player
    density = 1.0

    New()
        Move(locate(/area/entry))

    Bump(mob/other)
        world << ""Bumped""

/turf/wall
    density = 1.0

/area/entry

/turf/regular_floor

/turf/magic_floor
    Exited()
        new/turf/wall(src)
        return 1

/proc/count_magic_floor()
    var num_magic = 0
    for(var/turf/magic_floor/f)
        num_magic += 1

    return num_magic
";

        var dmmCode =
            @"
""x"" = (/turf/wall)
""a"" = (/turf/magic_floor)
""e"" = (/turf/regular_floor,/area/entry)
""f"" = (/turf/regular_floor)

(1,1,1) = {""
xxx
xex
xax
xfx
xfx
xxx
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var withPlayerScene = new SimpleSceneBuilder(sceneBuilder.Bounds, w =>
        {
            sceneBuilder.Build(w);
            var player = w.CreateEntity();
            entityBuilder.CreateAtomic(player, "/mob/player", "player");
            player.Set<PlayerCanImpersonateComponent>();
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(withPlayerScene, assembly);

        Stabalize(g);

        Assert.IsTrue((int)g.Environment.Global.ExecProc("count_magic_floor").CompleteOrException() == 1);

        var initialPos = DmlEnv.ParseCoord(g.GetHandle("player"));
        g.Slide("player", 0, 1);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.Count() == 0);
        var playerPos = DmlEnv.ParseCoord(g.GetHandle("player"));

        g.Slide("player", 0, -1);
        Stabalize(g);

        var interimPos = DmlEnv.ParseCoord(g.GetHandle("player"));
        Assert.IsTrue(interimPos.Equals(initialPos));

        g.Slide("player", 0, 1);
        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "Bumped"
        }));

        Assert.IsTrue((int)g.Environment.Global.ExecProc("count_magic_floor").CompleteOrException() == 0);
    }
}