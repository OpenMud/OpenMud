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
    public class ExceptionTest
    {

        [Test]
        public void TryCatchCaughtException()
        {
            var dmlCode =
                @"
proc/test_proc()
    throw 8 + 2

proc/test_trycatch()
    try
        throw 8 + 2
        return 1
    catch(var/e)
        return e + 12

    return 3
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test_trycatch").CompleteOrException();

            Assert.IsTrue(22 == r);
        }

        [Test]
        public void TryCatchCaughtExceptionThrowNested()
        {
            var dmlCode =
                @"
proc/test_proc()
    throw 8 + 2

proc/test_trycatch()
    try
        test_proc()
        return 1
    catch(var/e)
        return e + 12

    return 3
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test_trycatch").CompleteOrException();

            Assert.IsTrue(22 == r);
        }


        [Test]
        public void TryCatchCaughtExceptionThrowChained()
        {
            var dmlCode =
                @"
proc/test_proc()
    try
        throw 8 + 2
    catch(var/e)
        throw e * 2
proc/test_trycatch()
    try
        try
            test_proc()
            return 1
        catch(var/e)
            throw e + 12
    catch(var/e2)
        return e2 * 3
    return 3
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test_trycatch").CompleteOrException();

            Assert.IsTrue((((8 + 2) * 2) + 12) * 3 == r);
        }

        //Not caught
        //Not caught subtype
        //Caught subtype

    }


}
