using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class SystemTests
{
    [Test]
    public void WrapReturnTest()
    {
        var dmlCode =
            @"
/atom/test_atomic
    proc/test()
        return src
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/atom/test_atomic";
        var instance = system.CreateAtomic(name);

        var r = instance.ExecProc("test").CompleteOrException();

        Assert.IsTrue(r == instance);
    }
}