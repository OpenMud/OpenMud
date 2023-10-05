using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class StringExpressionsTest
{
    [Test]
    public void NestedQuotesTest()
    {
        var dmlCode =
            @"
/proc/test0()
    return ""Hello \""World\""""
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = (string)system.Global.ExecProc("test0").CompleteOrException();

        Assert.IsTrue(r == "Hello \"World\"");
    }
}