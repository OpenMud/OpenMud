using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class AtomicTests
{
    [DatapointSource] public Tuple<string, string>[] testSubjects =
    {
        Tuple.Create("obj", "/atom/movable/obj/test_atomic"),
        Tuple.Create("area", "/atom/area/test_atomic"),
        Tuple.Create("turf", "/atom/turf/test_atomic"),
        Tuple.Create("mob", "/atom/movable/mob/test_atomic"),
        Tuple.Create("atom", "/atom/test_atomic")
    };


    [Theory]
    public void TestDefaultFieldInit(Tuple<string, string> subject)
    {
        var dmlCode =
            @$"
/{subject.Item1}/test_atomic
    proc/test()
        return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = $"/{subject.Item1}/test_atomic";
        var instance = system.CreateAtomic(name);
        Assert.IsTrue(RuntimeTypeResolver.ExpandClassPath(instance["type"]) ==
                      RuntimeTypeResolver.ExpandClassPath(subject.Item2));
        Assert.IsTrue(instance["name"] == "test atomic");
    }


    [Theory]
    public void TestDefinedNameField(Tuple<string, string> subject)
    {
        var dmlCode =
            @$"
/{subject.Item1}/test_atomic
    name=""bob""
    proc/test()
        return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = $"/{subject.Item1}/test_atomic";
        var instance = system.CreateAtomic(name);
        Assert.IsTrue(instance["type"] == subject.Item2);
        Assert.IsTrue(instance["name"] == "bob");
    }


    [Theory]
    public void FalseyNull(Tuple<string, string> subject)
    {
        var dmlCode =
            @$"
/{subject.Item1}/test_atomic

/proc/falsy_null_test(a)
    if(a)
        return 1
    
    return 2
";

        //why//isn't this part being subbed: "if (ctx.op.Unary(ByondEcs.Environment.Operators.DmlUnary.Logical, localvar_a))"
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = $"/{subject.Item1}/test_atomic";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue(1 == (int)system.Global.ExecProc("falsy_null_test", instance).Result);
        Assert.IsTrue(2 == (int)system.Global.ExecProc("falsy_null_test", new object[] { null }).Result);
    }


    [Theory]
    public void ContentsImmutable(Tuple<string, string> subject)
    {
        var dmlCode =
            @$"
/{subject.Item1}/test_atomic

";

        //why//isn't this part being subbed: "if (ctx.op.Unary(ByondEcs.Environment.Operators.DmlUnary.Logical, localvar_a))"
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var failed = false;
        var name = $"/{subject.Item1}/test_atomic";
        var instance = system.CreateAtomic(name).Unwrap<Atom>();

        try
        {
            instance.contents.Assign(VarEnvObjectReference.CreateImmutable(null));
        }
        catch (Exception)
        {
            failed = true;
        }

        Assert.IsTrue(failed);
        Assert.IsTrue(!instance.contents.IsNull);
    }
}