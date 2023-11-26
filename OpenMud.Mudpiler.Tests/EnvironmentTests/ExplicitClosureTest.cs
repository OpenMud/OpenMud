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

        [Test]
        public void StatementListWithIfStatement0()
        {
            var dmlCode =
                @"
/proc/test(var/n)
    var/w = 10
    if (n == 0)
        w++;
    else
        w-=20;
        
    return w
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r1 = (int)system.Global.ExecProc("test", 0).CompleteOrException();
            Assert.IsTrue(r1 == 11);

            var r2 = (int)system.Global.ExecProc("test", 1).CompleteOrException();
            Assert.IsTrue(r2 == -10);
        }

        [Test]
        public void StatementListWithIfStatement1()
        {
            //Side note, this is a language feature that really makes no sense. But it seems simple single-statement code suites can optionally terminate with a semi-colon.
            //BUT these are not proper statement lists. For example, the following code would be invalid:
            // if(...) stmt1; stmt2; (NOT Valid because we have two statements...)
            // else ...
            //
            var dmlCode =
                @"
/proc/test(var/n)
    var/w = 10
    if (n == 0) w++;
    else w-=20;
        
    return w
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

            var r1 = (int)system.Global.ExecProc("test", 0).CompleteOrException();
            Assert.IsTrue(r1 == 11);

            var r2 = (int)system.Global.ExecProc("test", 1).CompleteOrException();
            Assert.IsTrue(r2 == -10);
        }
    }
}
