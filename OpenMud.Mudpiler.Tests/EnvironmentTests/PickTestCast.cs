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
    //The pick function has some fun syntax because why not.
    public class PickTestCast
    {
        [Test]
        public void MethodInstantiationTest0()
        {
            var dmlCode =
                @"
proc/test()
    //10 has a weight of 200
    //3 has a weight of 400
    //4 has a weight of 100

    return pick(200;10,400;3,4)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (int)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<int>() { 10, 3, 4 };
            Assert.IsTrue(possible.Contains(r));
        }


        [Test]
        public void MethodInstantiationTest1()
        {
            var dmlCode =
                @"
proc/test()
    //10 has a weight of 1200
    //3 has a weight of 100
    //4 has a weight of 100

    return pick(;10,3,4)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (int)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<int>() { 10, 3, 4 };
            Assert.IsTrue(possible.Contains(r));
        }


        [Test]
        public void MethodInstantiationTest4()
        {
            var dmlCode =
                @"
proc/test()
    //10 has a weight of 1200
    //3 has a weight of 100
    //4 has a weight of 100

    return pick(prob(200);10,3,4)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (int)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<int>() { 10, 3, 4 };
            Assert.IsTrue(possible.Contains(r));
        }


        [Test]
        public void MethodInstantiationTest5()
        {
            var dmlCode =
                @"
proc/test()
    //10 has a weight of 1200
    //3 has a weight of 100
    //4 has a weight of 100

    return pick(prob(200) 10,3,4)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (int)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<int>() { 10, 3, 4 };
            Assert.IsTrue(possible.Contains(r));
        }


        [Test]
        public void MethodInstantiationTest2()
        {
            var dmlCode =
                @"
proc/test()
    //10 has a weight of 1200
    //3 has a weight of 100
    //4 has a weight of 100

    return pick(10,3,4)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (int)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<int>() { 10, 3, 4 };
            Assert.IsTrue(possible.Contains(r));
        }

        [Test]
        public void MethodInstantiationTest3()
        {
            var dmlCode =
                @"
proc/test()
    //10 has a weight of 1200
    //3 has a weight of 100
    //4 has a weight of 100

    return pick(list(10,3,4))
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (int)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<int>() { 10, 3, 4 };
            Assert.IsTrue(possible.Contains(r));
        }

        [Test]
        public void MethodInstantiationTest6()
        {
            var dmlCode =
                @"
proc/test()
    return pick(""a"", ""cran-b"";5, ""c"";5, ""d"";5)
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r = (string)system.Global.ExecProc("test").CompleteOrException();

            var possible = new HashSet<string>() { "a", "b", "c", "d'" };
            Assert.IsTrue(possible.Contains(r));
        }
    }
}
