using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class InstantiationTests
{
    [Test]
    public void PropertyInitTestWithTmpModifier()
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
}