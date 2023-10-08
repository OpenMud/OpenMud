using System.Reflection;
using NUnit.Framework;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class HandleWrappingTests
{
    [Test]
    public void TestReturnWrapping()
    {
        var dmlCode =
            @"
/mob/test()
    return src
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mobTest = system.CreateAtomic("/mob");
        var r = mobTest.ExecProc("test").CompleteOrException();

        Assert.IsTrue(r is EntityHandle);
    }
}