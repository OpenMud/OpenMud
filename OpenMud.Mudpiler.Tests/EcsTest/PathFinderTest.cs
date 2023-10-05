using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Tests.EcsTest;

internal class PathFinderTest
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }


    [Test]
    public void SimpleFollowTest()
    {
        var dmlCode =
            @"
/mob/player
    density = 1.0

    New()
        Move(locate(/area/entry))
        world << ""spawn""

    verb
        goto_dest()
            walk_to(src, locate(/obj/flag))

/turf/wall
    density = 1.0

    Enter(t)
        world << ""Collision!""
        return 0

/area/entry

/area/checkpoint0
    Enter(t)
        world << ""c0""
        return 1

/area/checkpoint1
    Enter(t)
        world << ""c1""
        return 1

/area/checkpoint2
    Enter(t)
        world << ""c2""
        return 1

/area/flag0
    Enter(t)
        world << ""flag0""
        var flagObj = locate(/obj/flag)
        flagObj.Move(locate(/area/flag1).loc)
        return 1

/area/flag1
    Enter(t)
        world << ""flag1""
        return 1

/obj/flag
    density = 0
    New()
        Move(locate(/area/flag0).loc)

/turf/floor
";

        var dmmCode =
            @"
""x"" = (/turf/wall)
""p"" = (/mob/player,/turf/floor,/area/cave)
""a"" = (/turf/floor,/area/checkpoint0)
""b"" = (/turf/floor,/area/checkpoint1)
""c"" = (/turf/floor,/area/checkpoint2)
""d"" = (/turf/floor,/area/flag0)
""f"" = (/turf/floor,/area/flag1)
""e"" = (/turf/floor,/area/entry)

(1,1,1) = {""
xxxxxxxx
xbfcxxxx
xbxbxxxx
xbxaxxxx
xebcabdx
xxxxxxxx
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var withPlayerScene = new SimpleSceneBuilder(sceneBuilder.Bounds, w =>
        {
            sceneBuilder.Build(w);
            var player = w.CreateEntity();
            var flag = w.CreateEntity();
            entityBuilder.CreateAtomic(flag, "/obj/flag", "flag");
            entityBuilder.CreateAtomic(player, "/mob/player", "player");
            player.Set<PlayerCanImpersonateComponent>();
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(withPlayerScene, assembly);

        Stabalize(g);

        g.ExecuteVerb("player", "player", "goto_dest", new string[0]);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "spawn",
            "c1",
            "c2",
            "c0",
            "c1",
            "flag0",
            "c1",
            "c0",
            "c2",
            "c0",
            "c1",
            "c2",
            "flag1"
        }));
    }

    [Test]
    public void SimpleNavigationTest()
    {
        var dmlCode =
            @"
/mob/player
    density = 1.0

    New()
        Move(locate(/area/entry))
        world << ""spawn""

    verb
        goto_dest()
            walk_to(src, locate(/area/flag))

/turf/wall
    density = 1.0

    Enter(t)
        world << ""Collision!""
        return 0

/area/entry

/area/checkpoint0
    Enter(t)
        world << ""c0""
        return 1

/area/checkpoint1
    Enter(t)
        world << ""c1""
        return 1

/area/checkpoint2
    Enter(t)
        world << ""c2""
        return 1

/area/flag
    Enter(t)
        world << ""flag""
        return 1

/turf/floor
";

        var dmmCode =
            @"
""x"" = (/turf/wall)
""p"" = (/mob/player,/turf/floor,/area/cave)
""a"" = (/turf/floor,/area/checkpoint0)
""b"" = (/turf/floor,/area/checkpoint1)
""c"" = (/turf/floor,/area/checkpoint2)
""d"" = (/turf/floor,/area/flag)
""e"" = (/turf/floor,/area/entry)

(1,1,1) = {""
xxxxxxxx
xbbbxxxx
xbxbxxxx
xbxbxxxx
xebcabdx
xxxxxxxx
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

        g.ExecuteVerb("player", "player", "goto_dest", new string[0]);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "spawn",
            "c1",
            "c2",
            "c0",
            "c1",
            "flag"
        }));
    }
}