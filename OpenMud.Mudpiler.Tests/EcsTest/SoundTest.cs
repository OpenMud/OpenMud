using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Tests.EcsTest
{
    internal class SoundTest
    {

        private static void Stabalize(TestGame w)
        {
            for (var i = 0; i < 100; i++)
                w.Update(1);
        }


        [Test]
        public void AreaTriggerSoundTest()
        {
            var dmlCode =
                @"
/mob/player
    density = 1.0

    New()
        Move(locate(/area/entry))
        world << sound('global_sound.wav', repeat=1, channel=5)

/turf/wall
    density = 1.0

/area/entry
    Enter(t)
        return 1

/area/a0
    Enter(t)
        t << sound('scoped_sound.wav')
        return 1

/turf/floor
";

            var dmmCode =
                @"
""x"" = (/turf/wall)
""a"" = (/turf/floor,/area/a0)
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
            var initialLogicIdentifer = Guid.NewGuid();
            var withPlayerScene = new SimpleSceneBuilder(sceneBuilder.Bounds, w =>
            {
                sceneBuilder.Build(w);
                var player = w.CreateEntity();
                entityBuilder.CreateAtomic(player, "/mob/player", "player");
                player.Set(new LogicIdentifierComponent(initialLogicIdentifer));
                player.Set<PlayerCanImpersonateComponent>();
            });

            var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

            var g = new TestGame(withPlayerScene, assembly);

            Stabalize(g);

            Assert.IsTrue(g.EntitySoundConfig.Count == 0);
            var soundTrigger = g.WorldSoundConfig.Single();
            Assert.That(soundTrigger.Sound!, Is.EqualTo("global_sound.wav"));
            Assert.That(soundTrigger.Configuration, Is.EqualTo(SoundConfiguration.Loop));
            Assert.That(soundTrigger.Channel, Is.EqualTo(5));

            g.Slide("player", 0, 1);
            Stabalize(g);

            
            Assert.IsTrue(g.WorldSoundConfig.Count == 1);
            var soundTrigger2 = g.EntitySoundConfig["player"].Single();
            Assert.That(soundTrigger2.Sound!, Is.EqualTo("scoped_sound.wav"));
            Assert.That(soundTrigger2.Configuration, Is.EqualTo(SoundConfiguration.Play));
            Assert.That(soundTrigger2.Channel, Is.EqualTo(0));
        }
    }
}
