using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    internal class AssociativeListTest
    {
        [Test]
        public void AssocListInstantiationTest()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    materials[""wall""] = 1
    materials[""floor""] = 2
    materials[""door""] = 3

    return materials[""wall""] + materials[""floor""] + materials[""door""]
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 6);
        }

        [Test]
        public void AssocListInstantiationTest2()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    materials = list(
""wall"" = 1,
""floor"" = 2,
""door"" = 3)

    return materials[""wall""] + materials[""floor""] + materials[""door""]
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 6);
        }

        [Test]
        public void ContainsBehaviourTest0()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    materials[""wall""] = ""abc""

    return ""abc"" in materials
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            Assert.IsTrue(DmlEnv.AsLogical(system.Global.ExecProc("test0").CompleteOrException()) == false);
        }



        [Test]
        public void ContainsBehaviourTest1()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    materials[""wall""] = ""abc""

    return ""wall"" in materials
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            Assert.IsTrue(DmlEnv.AsLogical(system.Global.ExecProc("test0").CompleteOrException()) == true);
        }

        [Test]
        public void IteratesThroughKeysTest()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    materials[""wall""] = ""abc""
    materials[""chair""] = ""lhg""
    
    if(materials.len != 2)
        return -1

    var keylist = """"
    var valuelist = """"

    for(var/m in materials)
        keylist += m + "",""
        valuelist += materials[m] + "",""

    return keylist + "";"" + valuelist
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = DmlEnv.AsText(system.Global.ExecProc("test0").CompleteOrException());

            Assert.IsTrue(r == "wall,chair,;abc,lhg,");
        }



        [Test]
        public void NumericIndexRetrievesKeyTest()
        {
            var dmlCode =
                @"
var/materials[0]
/proc/test0()
    materials[""wall""] = ""abc""
    materials[""chair""] = ""lhg""
    
    return materials[1]
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = DmlEnv.AsText(system.Global.ExecProc("test0").CompleteOrException());

            Assert.IsTrue(r == "wall");
        }
    }
}
