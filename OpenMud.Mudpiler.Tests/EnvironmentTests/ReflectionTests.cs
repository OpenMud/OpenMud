using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using System.Reflection;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    internal class ReflectionTests
    {
        [Test]
        public void testMethodExpression()
        {
            var dmlCode =
                @$"
/proc/test_target(y, z)
    return y + z * y

/proc/test0()
    var methodRef = /proc/test_target

    return methodRef
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r0 = (Type)system.Global.ExecProc("test0").CompleteOrException();

            var names = r0.GetCustomAttributes<ProcDefinition>().Select(x => x.Name).ToHashSet();

            Assert.IsTrue(names.Contains("/proc/test_target"));
            Assert.IsTrue(names.Contains("/test_target"));
        }

        [Test]
        public void testIndirectInvoke_static()
        {
            var dmlCode =
                @$"
/proc/test_target(y, z)
    return y + z * y

/proc/test0()
    var methodRef = /proc/test_target

    return call(methodRef)(5, 8)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r0 = (int)system.Global.ExecProc("test0").CompleteOrException();

            Assert.IsTrue(r0 == 45);
        }

        [Test]
        public void testIndirectInvoke_instance()
        {
            var dmlCode =
                @$"
/mob/w
    var w = 8

    proc
        t0()
            w += 1

        t1(q)
            return 8 * q * w

/proc/test0()
    var/mob/w/t = new()
    var/mob/w/ti8 = new()
    t.t0()
    t.t0()
    return call(t, ""t1"")(7)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r0 = (int)system.Global.ExecProc("test0").CompleteOrException();

            Assert.IsTrue(r0 == 560);
        }

        [Test]
        public void testIndirectInvoke_instance_namedargs()
        {
            var dmlCode =
                @$"
/mob/w
    var w = 3

    proc
        t0()
            w += 1

        t1(q0, q1)
            return 8 * w + (q0 - q1)

/proc/test0()
    var/mob/w/t = new()
    var/mob/w/ti8 = new()
    t.t0()
    t.t0()
    return call(t, ""t1"")(q1=100, q0=1)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r0 = (int)system.Global.ExecProc("test0").CompleteOrException();

            Assert.IsTrue(r0 == (8*5) + 1 - 100);
        }
    }
}
