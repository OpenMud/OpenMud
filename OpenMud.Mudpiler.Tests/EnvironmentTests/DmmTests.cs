using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.Tests.EcsTest;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class DmmTests
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }

    [Test]
    public void TestDmmInitializers()
    {
        var dmlCode = @"
/turf/test
    var
        testvar1
        testvar2

    New()
        spawn(1)
            world << testvar1
            world << testvar2
";

        var dmmCode = @"
""a"" = (/turf/test{testvar1=10; testvar2=""test string""})

(1,1,1) = {""
a
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "10",
            "test string"
        }));
    }

    [Test]
    public void TestDmmLocationInitializers()
    {
        var dmlCode = @"
/turf/test
";

        var dmmCode = @"
""a"" = (/turf/test{layer=10})

(1,1,1) = {""
a
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        var subject = g.Environment.Actors
            .Where(a => RuntimeTypeResolver.InheritsBaseTypeDatum(a["type"], DmlPrimitiveBaseType.Turf)).Single();

        Assert.IsTrue(subject["layer"] == 10);
    }

    [Test]
    public void TestDmmTwoCharId()
    {
        var dmlCode = @"
/turf/test
    New()
        world << ""Spawned""
";

        var dmmCode = @"
""aa"" = (/turf/test)

(1,1,1) = {""
aa
""}
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new DmmSceneBuilderFactory(entityBuilder).Build(dmmCode);

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.SequenceEqual(new[]
        {
            "Spawned"
        }));
    }
}