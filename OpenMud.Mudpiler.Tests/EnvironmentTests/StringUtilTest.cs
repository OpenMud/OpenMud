using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using System.Reflection;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    public class StringUtilTest
    {
        [DatapointSource]
        public Tuple<string, object[], object>[] TEST_DATA =
        {
            Tuple.Create("findtext", new object[] {"HeLlO WoRlD", "WORLD"}, (object)7),
            Tuple.Create("findtext", new object[] {"HeLlO", "WORLD"}, (object)0),
            Tuple.Create("findtext", new object[] { "", "" },(object) 0),
            Tuple.Create("findtext", new object[] { "hello", "" },(object) 0),
            Tuple.Create("findtext", new object[] { "hello hello", "hello" },(object) 1),

            Tuple.Create("text", new object[] { "hello world"},(object) "hello world"),
            Tuple.Create("text", new object[] { "hello []"},(object) "hello "),
            Tuple.Create("text", new object[] { "hello []", 15},(object) "hello 15"),
            Tuple.Create("text", new object[] { "hello [] world []", 2, 1},(object) "hello 2 world 1"),

            Tuple.Create("text", new object[] { "hello [] world []", "text"},(object) "hello text world "),
            Tuple.Create("text", new object[] { "hello \\[] world []", "text"},(object) "hello [] world text"),
        };

        //Handle text() with '\'
        //escape//character
        [Theory]
        public void test(Tuple<string, object[], object> data)
        {
            var operandsEncoded = String.Join(",", data.Item2.Select(v =>
            {
                if (v is String s)
                    return $"\"{s}\"";

                return v.ToString();
            }));
            var testCode =
            @$"
/proc/test0()
    return {data.Item1}({operandsEncoded})
";
            var dmlCode = Preprocessor.Preprocess("testFile.dml", ".", testCode, null, null, EnvironmentConstants.BUILD_MACROS);
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r0 = system.Global.ExecProc("test0").CompleteOrException();

            dynamic? buffer = default;

            if (data.Item3.GetType() == typeof(int))
                buffer = DmlEnv.AsNumeric(r0);
            else
                buffer = DmlEnv.AsText(r0);

            Assert.IsTrue(((object)buffer).Equals(data.Item3));
        }
    }
}
