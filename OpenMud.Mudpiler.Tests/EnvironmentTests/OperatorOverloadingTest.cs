using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class OperatorOverloadingTest
{
    [Test]
    public void BinOverloadSubAddNegateCompile()
    {
        //Test all classes of operator overloading to make sure we don't get compile errors.
        var dmlCode =
            @"
/mob
    proc
        operator+(o)
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x + y
            return x

        operator-()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x + y
            return x

        operator-(a)
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x += -y
            return x

        operator+=(a)
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x += -y
            return x

        operator:=(a)
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x += -y
            return x

        operator++()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x += -y
            return x
";

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
    }

    [Test]
    public void UnIncPostPreTest()
    {
        //Test all classes of operator overloading to make sure we don't get compile errors.
        var dmlCode =
            @"
/mob
    var/num/x
    x = 0
    proc
        operator++(q)
            if(q == 1)
                x = x + 10
            else
                src.x = src.x + 50

proc/test_interact_post(a)
    a++

proc/test_interact_pre(a)
    ++a
";

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var test = system.CreateAtomic("/mob");

        system.Global.ExecProc("test_interact_post", test);
        system.Global.ExecProc("test_interact_post", test);
        Assert.IsTrue(test["x"] == 20);
        system.Global.ExecProc("test_interact_pre", test);
        system.Global.ExecProc("test_interact_pre", test);
        Assert.IsTrue(test["x"] == 120);
    }

    [Test]
    public void UnIncPrePostEvalTest()
    {
        //Test all classes of operator overloading to make sure we don't get compile errors.
        var dmlCode =
            @"
/mob
    var/num/x
    x = 0
    proc
        operator++(q)
            var n as num
            n = x
            x++
            
            if(q == 1)
                return n
            else
                return n + 1

proc/test_interact_post(a)
    a++
    a++
    a++

    return 10 + a++

proc/test_interact_pre(a)
    a++
    a++
    a++

    return 10 + ++a
";

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var test = system.CreateAtomic("/mob");

        Assert.AreEqual(13, (int)system.Global.ExecProc("test_interact_post", test).CompleteOrException());

        test["x"] = 0;

        Assert.AreEqual(14, (int)system.Global.ExecProc("test_interact_pre", test).CompleteOrException());
    }
}