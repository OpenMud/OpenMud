using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Tests.EcsTest;

public class DmmLoaderTest
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }

    [Test]
    public void TestMapParser()
    {
        var dmlCode =
            @"
/turf/wall
/area/cave

/mob/rat
/turf/floor
/obj/cheese
/area/outside
/obj/scroll
";

        var dmmCode =
            @"
""a"" = (/turf/wall,/area/cave)
""b"" = (/mob/rat,/turf/floor,/area/cave)
""c"" = (/turf/floor,/area/cave)
""d"" = (/obj/cheese,/turf/floor,/area/cave)
""e"" = (/turf/floor,/area/outside)
""f"" = (/obj/scroll,/turf/floor,/area/outside)

(1,1,1) = {""
aaaaaaaaaaa
abccccaccca
acccccadcca
acccaccccca
aaaaacaaaaa
eeeaaccccca
eeeeaaaaaca
eeefeeeaace
eeeeeeeeaae
eeeeeeeeeee
eeeeeeeeeee
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);
    }
}