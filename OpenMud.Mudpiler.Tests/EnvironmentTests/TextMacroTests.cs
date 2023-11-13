using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    public class TextMacroTests
    {

        [Test]
        public void TestAAn()
        {
            var testCode =
            @"
/proc/test()
    return ""\a [10]""
";
            var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null, EnvironmentConstants.BUILD_MACROS);
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (string)system.Global.ExecProc("test").CompleteOrException();

            Assert.IsTrue(r == @"\a 10");
        }
    }
}
