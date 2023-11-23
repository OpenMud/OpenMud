using OpenMud.Mudpiler.Compiler.Core;
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
    public class PrimitiveTypeAssertionTests
    {
        [Test]
        public void AssertTextual()
        {
            var dmlCode =
                @"
/proc/test_fail()
    return 1234 as text

/proc/test_pass0()
    return ""1234"" as text
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            try
            {
                system.Global.ExecProc("test_fail").CompleteOrException();
                Assert.Fail();
            }
            catch (DmlRuntimeAssertionError ex)
            {
            }


            try
            {
                var r = (string)system.Global.ExecProc("test_pass0").CompleteOrException();
                Assert.IsTrue(r == "1234");
            }
            catch (DmlRuntimeAssertionError ex)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void AssertNumeric()
        {
            var dmlCode =
                @"
/proc/test_fail()
    return ""1234"" as num

/proc/test_pass0()
    return 1234 as num

/proc/test_pass1()
    return 1234.0 as num
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            try
            {
                system.Global.ExecProc("test_fail").CompleteOrException();
                Assert.Fail();
            }
            catch (DmlRuntimeAssertionError ex)
            {
            }


            try
            {
                var r = (int)system.Global.ExecProc("test_pass0").CompleteOrException();
                Assert.IsTrue(r == 1234);

                r = (int)system.Global.ExecProc("test_pass1").CompleteOrException();
                Assert.IsTrue(r == 1234);
            }
            catch (DmlRuntimeAssertionError ex)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void AssertNull()
        {
            var dmlCode =
                @"
/proc/test_fail()
    return ""1234"" as null

/proc/test_pass0()
    return null as null
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            try
            {
                system.Global.ExecProc("test_fail").CompleteOrException();
                Assert.Fail();
            }
            catch (DmlRuntimeAssertionError ex)
            {
            }


            try
            {
                var r = (object)system.Global.ExecProc("test_pass0").CompleteOrException();
                Assert.IsTrue(r == null);
            }
            catch (DmlRuntimeAssertionError ex)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComboTest()
        {
            var dmlCode =
                @"
/proc/test_fail()
    return null as num | text

/proc/test_pass0()
    return 1234 as num | text

/proc/test_inner()
    return ""1234""

/proc/test_pass1()
    var w = test_inner() as num|text
    return w
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            try
            {
                system.Global.ExecProc("test_fail").CompleteOrException();
                Assert.Fail();
            }
            catch (DmlRuntimeAssertionError ex)
            {
            }


            try
            {
                var r = (int)system.Global.ExecProc("test_pass0").CompleteOrException();
                Assert.IsTrue(r == 1234);

                var r2 = (string)system.Global.ExecProc("test_pass1").CompleteOrException();
                Assert.IsTrue(r2 == "1234");
            }
            catch (DmlRuntimeAssertionError ex)
            {
                Assert.Fail();
            }
        }
        /*
         * Temporarily disabling this test.
        [Test]
        public void ComboForListTest()
        {
            var dmlCode =
                @"
/proc/test_fail()
    var w = list(null, ""test"", 123)

    for(var/i as null | text in w)
        var l = i

/proc/test_pass0()
    var w = list(null, ""test"", ""123"")

    for(var/i as null | text in w)
        var l = i
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            try
            {
                system.Global.ExecProc("test_fail").CompleteOrException();
                Assert.Fail();
            }
            catch (DmlRuntimeAssertionError ex)
            {
            }


            try
            {
                var r = (int)system.Global.ExecProc("test_pass0").CompleteOrException();
            }
            catch (DmlRuntimeAssertionError ex)
            {
                Assert.Fail();
            }
        }*/

    }
}
