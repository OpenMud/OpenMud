using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

//Operator overloading... because why not.
public class OperationsTests
{
    [Test]
    public void TestFalsyZero()
    {
        var dmlCode =
            @"
proc
    test0()
        var n = 0.00
        if(n)
            return 1
        else
            return 0
    test1()
        var n = 0.0001
        if(n)
            return 1
        else
            return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 0);
        Assert.IsTrue((int)system.Global.ExecProc("test1").CompleteOrException() == 1);
    }

    [Test]
    public void TestStandAloneExpression()
    {
        var dmlCode =
            @"
proc
    test0()
        1 + 25
        45 * 6
        8 / 8
        return 5
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 5);
    }

    [Test]
    public void TestIntegerBitwiseOr()
    {
        var dmlCode =
            @"
proc
    test0()
        return 1 | 2 | 4
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == (1 | 2 | 4));
    }

    [Test]
    public void BasicAddition()
    {
        var dmlCode =
            @"
/mob
    proc
        test0()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x + y
            return x

        test1()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x += y
            return x

        test2()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x + -y
            return x

        test3()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x += -y
            return x
";
        var results = new[]
        {
            12,
            12,
            8,
            8
        };
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        for (var i = 0; i < results.Length; i++)
            Assert.IsTrue((int)mob.ExecProc($"test{i}").CompleteOrException() == results[i]);
    }

    [Test]
    public void BasicSubtraction()
    {
        var dmlCode =
            @"
/mob
    proc
        test0()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x - y
            return x

        test1()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x -= y
            return x

        test2()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x - -y
            return x

        test3()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x -= -y
            return x
";
        var results = new[]
        {
            8,
            8,
            12,
            12
        };
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        for (var i = 0; i < results.Length; i++)
            Assert.IsTrue((int)mob.ExecProc($"test{i}").CompleteOrException() == results[i]);
    }

    [Test]
    public void OrderOfOperationsTest()
    {
        var dmlCode =
            @"
/mob
    proc
        test0()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = 2 + x * y
            return x

        test1()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x * y + 2
            return x

        test2()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = (2 + x) * y
            return x

        test3()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = x * (y + 2)
            return x

        test4()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = -x * (y + 2)
            return x

        test5()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x *= y + 2
            return x

        test6()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x = -x * -y
            return x

        test7()
            var t1 as num
            var x as num
            var y as num
            x = 10
            y = 2
            x *= -y
            return x
";
        var results = new[]
        {
            22,
            22,
            24,
            40,
            -40,
            40,
            20,
            -20
        };
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        for (var i = 0; i < results.Length; i++)
            Assert.IsTrue((int)mob.ExecProc($"test{i}").CompleteOrException() == results[i]);
    }

    [Test]
    public void NullTest()
    {
        var dmlCode =
            @"
/mob
    proc
        test0()
            var x = null
            if(x)
                return 0

            return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");
        Assert.IsTrue((int)mob.ExecProc("test0").CompleteOrException() == 1);
    }



    [Test]
    public void ScientificNotationTest()
    {
        var dmlCode =
            @"
/mob
    var
        test_value = 1e31
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob");

        var testVal = (double)mob["test_value"];
        var expected = 1E31;

        Assert.IsTrue(testVal - expected < 0.0001f);
    }
}