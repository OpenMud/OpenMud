using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class ObjectTreePathsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void EmbeddedRootedPathTest()
    {
        var dmlCode =
            @"
turf
    var tx as num
    tx = 1
    trap
        tx = 2
        /turf/trap/pit
            tx = 3
";
        var tests = new List<(string obj, int val)>
        {
            ("/turf/trap/pit", 3)
        };

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        foreach (var (obj, val) in tests)
        {
            var asm = system.CreateAtomic(obj);
            Assert.True((int)asm["tx"] == val);
        }
    }


    [Test]
    public void SingleLineObjDeclarationTest()
    {
        var dmlCode =
            @"
/obj/test
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        Assert.IsTrue(null != system.CreateAtomic("/obj/test"));
    }

    [Test]
    public void SingleLineObjDeclarationWithHangingForwardSlashTest()
    {
        var dmlCode =
            @"
/obj/test/
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        Assert.IsTrue(null != system.CreateAtomic("/obj/test"));
    }

    [Test]
    public void SingleLineObjFieldDeclaration()
    {
        var dmlCode =
            @"
obj
	test
		var
			testfield

/obj/test/testfield = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var o = system.CreateAtomic("/obj/test");
        Assert.IsTrue((int)o["testfield"] == 10);
    }



    [Test]
    public void GlobalVarDeclaration()
    {
        var dmlCode =
            @"
var/global/testfield = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var o = system.Global["testfield"];
        Assert.IsTrue(o == 10);
    }

    [Test]
    public void GlobalVarDeclaration2()
    {
        var dmlCode =
            @"
/var/global/testfield = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var o = system.Global["testfield"];
        Assert.IsTrue(o == 10);
    }

    [Test]
    public void ObjDeclarationWithHangingForwardSlashTest()
    {
        var dmlCode =
            @"
/obj/test/
    var
        t = 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var o = system.CreateAtomic("/obj/test");
        Assert.IsTrue((int)o["t"] == 10);
    }

    [Test]
    public void EmptyObjDeclarationTest()
    {
        var dmlCode =
            @"
area
	outside

	cave
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        Assert.IsTrue(null != system.CreateAtomic("/area/outside"));
        Assert.IsTrue(null != system.CreateAtomic("/area/cave"));
    }

    [Test]
    public void FieldDeclarationBlock()
    {
        var dmlCode =
            @"
area
	var
		music = 10
		AAA
		varB
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.CreateAtomic("/area")["music"] == 10);
        Assert.IsTrue(system.CreateAtomic("/area")["AAA"] == null);
        Assert.IsTrue(system.CreateAtomic("/area")["varB"] == null);
    }

    [Test]
    public void EmptyMethodDeclaration()
    {
        var dmlCode =
            @"
area
    test
        Bump(atom/obstacle)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue(system.CreateAtomic("/area/test").ExecProc("Bump").CompleteOrException() == null);
    }




    [Test]
    public void GlobalVarSetDeclaration()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
var/
    w = 10
    q = 20

/proc/test0()

    return w + q
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 30);
    }

    [Test]
    public void LocalVarSetDeclaration()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"

/proc/test0()
    var
        w = 10
        q = 20

    return w + q
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test0").CompleteOrException();
        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 30);
    }

    [Test]
    public void InitializeGlobalVariableComplexExpressionTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"

/proc/test(w)
    return w * w

var/global/test_global = test(20)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global["test_global"];
        Assert.IsTrue(r == 20 * 20);
    }

    [Test]
    public void VariableListDeclarationTest0()
    {
        var dmlCode =
            @"
/turf
    var mob/fa, /obj/fq

    test_proc()
        var mob/a, obj/b, c = 8
        a = new
        b = new
        fa = new
        fq = new
        
        if(!istype(a, /mob))
            return -1

        if(!istype(b, /obj))
            return -2

        if(!istype(fa, /mob))
            return -3

        if(!istype(fq, /obj))
            return -4

        if(c != 8)
            return -5

        return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var o = system.CreateAtomic("/turf");
        var r = o.ExecProc("test_proc").CompleteOrException();
        Assert.IsTrue((int)r == 1);
    }

    [Test]
    public void VariableListDeclarationTest1()
    {
        var dmlCode =
            @"
/proc/test_global()
    var a = 8, b = 3

    return a + b
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_global").CompleteOrException();
        Assert.IsTrue(r == 8 + 3);
    }

    [Test]
    public void VariableListDeclarationTest2()
    {
        var dmlCode =
            @"
/turf
    /var/w
    New(n)
        w = n

/proc/test_global()
    var /turf/a = new(8), /turf/b = new(3)

    return a.w + b.w
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var r = (int)system.Global.ExecProc("test_global").CompleteOrException();
        Assert.IsTrue(r == 8 + 3);
    }
}