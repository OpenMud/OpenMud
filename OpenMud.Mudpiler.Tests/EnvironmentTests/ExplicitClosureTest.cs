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
    public class ExplicitClosureTest
    {

        [Test]
        public void StatementList()
        {
            var dmlCode =
                @"
/proc/test()
    var w = 0
    w++; w++; w++;
    return w;
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test").CompleteOrException();
            Assert.IsTrue(r == 3);
        }

        [Test]
        public void StatementList1()
        {
            var dmlCode =
                @"
/proc/test()
    var w = 10
    if(w==0);w++;
    return w;
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test").CompleteOrException();
            Assert.IsTrue(r == 11);
        }

        [Test]
        public void StatementList2()
        {
            var dmlCode =
                @"
/proc/test()
        var/w = 1

        if(w==0);w++
        if(w == w) {w++; w++; w++}
        if(w == w) {
            w++; w++; w++
        }

        if(w == w)
        {
            w++; w++; w++}
		
		
        if(w == w)
        {w++; w++; w++}

        if(w == w)
        {
            w++; w++; w++
        }

        return w
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var r = (int)system.Global.ExecProc("test").CompleteOrException();
            Assert.IsTrue(r == 17);
        }
    }
}
