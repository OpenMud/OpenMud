using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Tests.EcsTest
{
    internal class VerbInteractionTest
    {

        private static void Stabalize(TestGame w)
        {
            for (var i = 0; i < 100; i++)
                w.Update(1);
        }


        [Test]
        public void Oview_0_VerbInteraction()
        {
            var dmlCode =
                @"

/area/entry

/turf/floor

/mob/player
    density = 1.0

    New()
        Move(locate(/area/entry))
        world << ""spawn""

    verb
        goto_dest()
            walk_to(src, locate(/obj/scroll))

/turf/wall
    density = 1.0

    Enter(t)
        world << ""Collision!""
        return 0

obj
    verb
        get()
            set src in oview(0)
            world << ""You get scroll.""
            Move(usr)

obj
    scroll
        name = ""scroll""
";

            var dmmCode =
                @"
""x"" = (/turf/wall)
""."" = (/turf/floor)
""e"" = (/turf/floor,/area/entry)
""s"" = (/turf/floor,/obj/scroll)
(1,1,1) = {""
xxxxx
x..sx
xe..x
xxxxx
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

            var instance_name = g.GetInstanceNameByObjectDisplayName("scroll");

            //This one will fail, player not near scroll.
            g.ExecuteVerb("player", instance_name, "get", new string[0]);
            Stabalize(g);

            Assert.IsTrue(g.WorldMessages.SequenceEqual(new[] {
            "spawn",
            }));
            
            Assert.IsTrue(g.VerbRejectionMessages.Single().Source == "player");

            g.ExecuteVerb("player", "player", "goto_dest", new string[0]);
            Stabalize(g);

            //This one will succeed, player is on top of scroll.
            g.ExecuteVerb("player", instance_name, "get", new string[0]);

            Stabalize(g);
            Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
            {
            "spawn",
            "You get scroll."
            }));

            Assert.IsTrue(g.VerbRejectionMessages.Count == 1);
        }

    }
}
