using System.Reflection;
using DefaultEcs;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EcsTest;

public class ObjectsTest
{
    private static void Stabalize(TestGame w)
    {
        for (var i = 0; i < 100; i++)
            w.Update(1);
    }

    [Test]
    public void ObjectCreationTest()
    {
        var dmlCode =
            @"
/mob/bob
    verb
        sayHello()
            world << ""Hello World""
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new TestSceneBuilder(w =>
        {
            entityBuilder.CreateAtomic(w.CreateEntity(), "/mob/bob", "test_mob_instance");
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        g.ExecuteVerb("test_mob_instance", "test_mob_instance", "sayhello", new string[] { });

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.Single() == "Hello World");
    }

    [Test]
    public void EntityEchoTest()
    {
        var dmlCode =
            @"
/mob/bob
    verb
        sayHello()
            src << ""Hello World""
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new TestSceneBuilder(w =>
        {
            entityBuilder.CreateAtomic(w.CreateEntity(), "/mob/bob", "test_mob_instance");
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        g.ExecuteVerb("test_mob_instance", "test_mob_instance", "sayhello", new string[] { });

        Stabalize(g);

        Assert.IsTrue(g.EntityMessages["test_mob_instance"].Single() == "Hello World");
    }

    [Test]
    public void WorldEchoTest()
    {
        var dmlCode =
            @"
/mob/bob
    verb
        sayHello()
            world << ""Hello World""
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new TestSceneBuilder(w =>
        {
            entityBuilder.CreateAtomic(w.CreateEntity(), "/mob/bob", "test_mob_instance");
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        g.ExecuteVerb("test_mob_instance", "test_mob_instance", "sayhello", new string[] { });

        Stabalize(g);

        Assert.IsTrue(g.WorldMessages.Single() == "Hello World");
    }


    [Test]
    public void MobNewOnLayerTest()
    {
        var dmlCode =
            @"
/mob

/proc/test_create()
    return new/mob(layer=1234)
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new TestSceneBuilder(w =>
        {
            entityBuilder.CreateAtomic(w.CreateEntity(), "/mob", "test_mob_instance");
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        var h = g.GetHandle("test_mob_instance");

        var w = (EntityHandle)h.ExecProc("test_create").CompleteOrException();

        Stabalize(g);

        Assert.IsTrue((int)w["layer"] == 1234);
    }


    [Test]
    public void MobInitializeLayer()
    {
        var dmlCode =
            @"
/mob
    layer = 10
";
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = new TestSceneBuilder(w =>
        {
            entityBuilder.CreateAtomic(w.CreateEntity(), "/mob", "test_mob_instance");
        });

        var assembly = Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCode));

        var g = new TestGame(sceneBuilder, assembly);

        Stabalize(g);

        var h = g.GetHandle("test_mob_instance");

        Stabalize(g);

        Assert.IsTrue((int)h["layer"] == 10);
    }

    private class TestSceneBuilder : IMudSceneBuilder
    {
        private readonly Action<World> builder;

        public TestSceneBuilder(Action<World> builder)
        {
            this.builder = builder;
        }

        public void Build(World world)
        {
            builder(world);
        }
    }
}