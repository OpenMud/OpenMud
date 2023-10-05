using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Tests.EcsTest;

internal class BumpTest
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }


    [Test]
    public void SimpleBumpTest()
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

/area/checkpoint0
    Enter(t)
        world << ""c0""
        return 1


/turf/floor
";

        var dmmCode =
            @"
""x"" = (/turf/wall)
""a"" = (/turf/floor,/area/checkpoint0)
""e"" = (/turf/floor,/area/entry)

(1,1,1) = {""
xxx
xex
xax
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

        g.Slide("player", 0, 1);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "spawn",
            "c0"
        }));

        g.Slide("player", 0, 1);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "spawn",
            "c0",
            "Bumped"
        }));
    }
}