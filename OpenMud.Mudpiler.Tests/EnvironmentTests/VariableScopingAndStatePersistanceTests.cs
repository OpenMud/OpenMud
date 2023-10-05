using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class VariableScopingAndStatePersistanceTests
{
    [Test]
    public void TestGlobalInit()
    {
        var dmlCode =
            @"
var/num/dest_global  = -1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.True(
            (int)system.Global["dest_global"] == -1
        );
    }


    [Test]
    public void TestGlobalLocalNamespaceResolution()
    {
        var dmlCode =
            @"
/turf/testcase
    var test_local2 as num = 1000
    var test_local as num = 100
    proc/testfunc()
        var bob as num
        dest_global=dest_global * (test_local + test_local2 + test_global)

    proc/testget() as num
        return dest_global

var/num/test_global = 10
var/num/test_local  = 1
var/num/dest_global  = -1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var testMob = system.CreateAtomic("/turf/testcase");

        Assert.True(
            (int)testMob.ExecProc("testget").CompleteOrException() == -1
        );

        testMob.ExecProc("testfunc");

        Assert.True(
            (int)testMob.ExecProc("testget").CompleteOrException() == -1110
        );
    }


    [Test]
    public void TestVariableInheritance()
    {
        var dmlCode =
            @"
turf
    var w = 0

/turf/testcase
    proc/testfunc()
        w = 20

var/num/w = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var test = system.CreateAtomic("/turf/testcase");

        Assert.True(
            (int)test["w"] == 0
        );

        Assert.True(
            (int)system.Global["w"] == 10
        );

        test.ExecProc("testfunc");

        Assert.True(
            (int)test["w"] == 20
        );

        Assert.True(
            (int)system.Global["w"] == 10
        );
    }


    [Test]
    public void GlobalFieldPersistance()
    {
        var dmlCode =
            @"
obj/magic_paper
   var/global/msg

   verb
      write(txt as text)
         msg = txt
      read()
         return msg
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var magicPaperA = system.CreateAtomic("/obj/magic_paper");
        var magicPaperB = system.CreateAtomic("/obj/magic_paper");

        magicPaperA.Interact(magicPaperA, "write", "Hello World");
        Assert.IsTrue(magicPaperA.Interact(magicPaperA, "read").CompleteOrException() == "Hello World");
        Assert.IsTrue(magicPaperB.Interact(magicPaperB, "read").CompleteOrException() == "Hello World");
    }


    [Test]
    public void LocalFieldIndependence()
    {
        var dmlCode =
            @"
obj/unmagic_paper
   var/msg

   verb
      write(txt as text)
         msg = txt
      read()
         return msg
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var magicPaperA = system.CreateAtomic("/obj/unmagic_paper");
        var magicPaperB = system.CreateAtomic("/obj/unmagic_paper");

        magicPaperA.Interact(magicPaperA, "write", "TestA");
        magicPaperB.Interact(magicPaperB, "write", "TestB");
        Assert.IsTrue(magicPaperA.Interact(magicPaperA, "read").CompleteOrException() == "TestA");
        Assert.IsTrue(magicPaperB.Interact(magicPaperB, "read").CompleteOrException() == "TestB");
    }

    [Test]
    public void PropertyVarTreeWithTmpModifier()
    {
        var dmlCode =
            @"
// around and display some form of intelligent behavior.
mob
    var/tmp
        has_hands = 1   // Test comment
        wielded         // Test comment
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var turfsub = system.CreateAtomic("/mob");

        Assert.IsTrue((int)turfsub["has_hands"] == 1);
    }


    [Test]
    public void PropertyInitTestWithGlobalModifier()
    {
        var dmlCode =
            @"
// around and display some form of intelligent behavior.
mob
    var/global
        has_hands = 1   // Test comment
        wielded         // Test comment

    test_proc()
        has_hands = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var mob_a = system.CreateAtomic("/mob");
        var mob_b = system.CreateAtomic("/mob");

        Assert.IsTrue((int)mob_a["has_hands"] == 1);
        Assert.IsTrue((int)mob_b["has_hands"] == 1);

        mob_a.ExecProc("test_proc").CompleteOrException();

        Assert.IsTrue((int)mob_a["has_hands"] == 10);
        Assert.IsTrue((int)mob_b["has_hands"] == 10);
    }

    [Test]
    public void SingleLineVarDeclaration()
    {
        var dmlCode =
            @"
// around and display some form of intelligent behavior.
mob
    var/mob/testmob    //this is a mob

";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
    }

    [Test]
    public void VarTreeDeclareFieldWithType()
    {
        var dmlCode = @"
mob/lerf
    var/global
        list/barrel_list
        lerf_count = 0

    New()
        if(!barrel_list)
            barrel_list = new()
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var w = system.CreateAtomic("/mob/lerf");

        Assert.IsTrue(((DatumHandle)w["barrel_list"]).Unwrap<DmlList>() != null);
    }
}