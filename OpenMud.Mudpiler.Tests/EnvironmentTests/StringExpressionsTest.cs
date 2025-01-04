using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
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

    [Test]
    public void StringExpressionInFieldTest()
    {
        var testCode =
        @"
/mob/bob
    desc = ""this is [""b"" + ""ob""]""

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var bob = system.CreateAtomic("/mob/bob");
        Assert.IsTrue((string)bob["desc"] == "this is bob");
    }

    [Test]
    public void EscapedStringLiteralInFieldTest1()
    {
        var testCode =
        @"
/mob/bob
    desc = ""\""this\"" is a string""

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var t = system.CreateAtomic("/mob/bob");
        Assert.IsTrue(t["desc"] == "\"this\" is a string");
    }

    [Test]
    public void TestStringLiteralWithPercent()
    {
        var testCode =
        @"
/mob/bob
    desc = ""%this% is a string""

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var t = system.CreateAtomic("/mob/bob");
        Assert.IsTrue(t["desc"] == "%this% is a string");
    }

    [Test]
    public void TestStringLiteralWithSlash()
    {
        var testCode =
        @"
/mob/bob
    desc = ""icons\\rat.dmi""

";
        var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var t = system.CreateAtomic("/mob/bob");
        Assert.IsTrue(t["desc"] == "icons\\rat.dmi");
    }

}