using System.Reflection;
using Microsoft.Toolkit.HighPerformance.Helpers;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Helper;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class DebugTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestStepEvent()
    {
        var rawDmlCode =
            @"
proc/test_if()
    var/x = 10
    x = x * 2
    x = x + 1
    if(test_input == 0)
        x = x + 1
        return 5
    else
        x = x + 2
        return 10

var/num/test_input = 10
";
        IImmutableSourceFileDocument dmlCode = SourceFileDocument.Create("test.dm", 0, rawDmlCode, false);
        var assembly = MsBuildDmlCompiler.Compile(
            dmlCode.AsPlainText(true),
            debuggable: true
        );
        
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var r = system.TypeSolver.Lookup("/proc/test_if");

        var line_states = new List<Tuple<int, object>[]>();
        var executed = new List<Tuple<int, object>>();
        
        DebuggableProcHelper.RegisterStep(r, (ctx, line) =>
        {
            executed.Add(Tuple.Create(line, ctx.ImmutableLocalScope["x"].GetOrDefault<object>(null)));
            line_states.Add(executed.ToArray());
        });

        var info = DebuggableProcHelper.CollectDebugInformation(r);

        var result = (int)system.Global.ExecProc("test_if").CompleteOrException();
        Assert.IsTrue(10 == result);

        var expectedResult = new List<Tuple<int, object>[]>()
        {
            new[] {Tuple.Create<int, object>(3, null)},
            new[] {Tuple.Create<int, object>(3, null), Tuple.Create<int, object>(4, 10)},
            new[] {Tuple.Create<int, object>(3, null), Tuple.Create<int, object>(4, 10), Tuple.Create<int, object>(5, 20)},
            new[] {Tuple.Create<int, object>(3, null), Tuple.Create<int, object>(4, 10), Tuple.Create<int, object>(5, 20), Tuple.Create<int, object>(6, 21)},
            new[] {Tuple.Create<int, object>(3, null), Tuple.Create<int, object>(4, 10), Tuple.Create<int, object>(5, 20), Tuple.Create<int, object>(6, 21), Tuple.Create<int, object>(10, 21)},
            new[] {Tuple.Create<int, object>(3, null), Tuple.Create<int, object>(4, 10), Tuple.Create<int, object>(5, 20), Tuple.Create<int, object>(6, 21), Tuple.Create<int, object>(10, 21), Tuple.Create<int, object>(11, 23)},
        };

        Assert.IsTrue(expectedResult.Zip(line_states).All(
            a => Enumerable.SequenceEqual(a.First, a.Second)
        ));
    }
}