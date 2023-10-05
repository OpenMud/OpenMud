using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EcsTest;

internal class BasicFrameworkTest
{
    [Test]
    public void TurnTest()
    {
        var dmlCode =
            @"
/proc/turntest()
    var/dir
    dir = turn(NORTH, 90)  // dir = west

    if(dir != WEST)
        return 0

    dir = turn(dir, -90)   // dir = north

    if(dir != NORTH)
        return 0

    dir = turn(dir, 45)    // dir = northwest

    if(dir != NORTHWEST)
        return 0

    return 1
";
        dmlCode = Preprocessor.Preprocess(".", dmlCode, (a, b) => throw new NotImplementedException(),
            (a, b, c, d) => throw new Exception("Import not supported"), EnvironmentConstants.BUILD_MACROS);

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);

        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("turntest").CompleteOrException() == 1);
    }
}