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
        //This should fail, field initiailizers must be constant expressions
        //PLUS the fields are inited by the special _constructor method, which does not run in a proper
        //DmlMethod and thus has no execution context to resolve method calls.
        var testCode =
        @"
/mob/bob
    desc = ""this is [""b"" + ""ob""]""

";
        try
        {
            var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null);
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            Assert.Fail();
        }
        catch (Exception ex)
        {
            //This is a pass, build should fail.
            return;
        }
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
}