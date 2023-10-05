using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class ListTests
{
    [Test]
    public void ListInstantiationAndTypeTest()
    {
        var dmlCode =
            @"
/proc/test0()
    var/list/mylist_a
    var/mylist_b[]
    var/mylist_c[][][]
    if(mylist_a || mylist_b || mylist_c)
        return 1
    else
        return 0

/proc/test1()
    var/list/mylist_a
    var/mylist_b[]
    var/mylist_c[][][]

    if(istype(mylist_a) || istype(mylist_b) || istype(mylist_c))
        return 0

    mylist_a = new/list(1)
    mylist_b = new/list(1)
    mylist_c = new/list(1)

    if(!istype(mylist_a) || !istype(mylist_b) || !istype(mylist_c))
        return 0
    
    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 0);
        Assert.IsTrue((int)system.Global.ExecProc("test1").CompleteOrException() == 1);
    }

    [Test]
    public void TestInitializerHigherPrec()
    {
        var dmlCode =
            @"
/proc/test0()
    //Am I here?
    var/list/e[1][5][] = 8

    if(istype(e))
        return 0

    if(e != 8)
        return 0

    e = new/list(1)

    if(!istype(e))
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void TestComputedArraySize()
    {
        var dmlCode =
            @"
/proc/test0()
    var n = 2
    var/list/e[n*n]

    if(!istype(e))
        return -1
    
    e[1] = 6
    e[n] = 4
    e[n] += 3
    e[n + 1] = e[n] * 2
    e[n * 2] = 8

    return e[1] + e[2] + e[3] + e[4]
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 6 + 7 + 7 * 2 + 8);
    }

    [Test]
    public void ListDeclarationVariations()
    {
        var dmlCode =
            @"
/proc/test0()
    var/list/mylist_a
    var/list/mylist_aa[][][][]
    var/mylist_b[]
    var/mylist_c = new /list(6)
    var/mylist_d[6]

    var n = 10
    var/mylist_e[n][n * 2][n+1]
    
    //Am I here?
    var/list/mylist_f[1][5][] = 8

    if(!mylist_f || !mylist_e || !mylist_d || !mylist_c)
        return 0

    if(mylist_aa || mylist_a || mylist_b)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListInitTest()
    {
        var dmlCode =
            @"
/proc/test0()
    var/list/mylist_a[1]
    return mylist_a.len == 1 && !mylist_a[1]

/proc/test1()
    var/list/a[1][5][]
    return a.len == 1 && a[1].len == 5 && a[1][1].len == 0 && a[1]
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((bool)system.Global.ExecProc("test0").CompleteOrException());
        Assert.IsTrue((bool)system.Global.ExecProc("test1").CompleteOrException());
    }

    [Test]
    public void ListInTest()
    {
        var dmlCode =
            @"
/proc/test0()
    var/list/a[1]
    
    if(5 in a)
        return 0

    a[1] = 5

    if(5 in a)
        return 1

    return 0
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListRemoveSingleItemTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[0]
    
    a.Add(5)
    a.Add(4)
    a.Add(3)
    a.Add(2)
    a.Add(5)
    a.Add(1)
    a.Add(5)

    if(a.len != 7)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] + a[7] != 25)
        return 0

    if(a.Remove(5) != 1)
        return 0

    if(a.len != 6)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] != 20)
        return 0

    if(a[1] != 5 || a[5] != 5)
        return 0

    a.Remove(5)

    if(a[1] != 5 || a[5] == 5)
        return 0

    a.Remove(5)

    if(a.Remove(5) != 0)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void SimpleListLiteralTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[] = list(1,2,3)

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListLiteralTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[] = list(1,2,3)

    if(a[1] + a[2] + a[3] != 6)
        return 0
    
    a = list(5, 4, 3, 2, 5, 1, 5)

    if(a.len != 7)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] + a[7] != 25)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListRemoveSingleItemWithOperatorTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[0]
    
    a.Add(5)
    a.Add(4)
    a.Add(3)
    a.Add(2)
    a.Add(5)
    a.Add(1)
    a.Add(5)

    if(a.len != 7)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] + a[7] != 25)
        return 0

    a -= 5

    if(a.len != 6)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] != 20)
        return 0

    if(a[1] != 5 || a[5] != 5)
        return 0

    a -= 5

    if(a[1] != 5 || a[5] == 5)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListAddSingleItemWithOperatorTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[0]
    
    a += 5
    a += 4
    a += 3
    a += 2
    a += 5
    a += 1
    a += 5

    if(a.len != 7)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] + a[7] != 25)
        return 0

    a -= 5

    if(a.len != 6)
        return 0

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] != 20)
        return 0

    if(a[1] != 5 || a[5] != 5)
        return 0

    a -= 5

    if(a[1] != 5 || a[5] == 5)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListConcatSingleItemWithOperatorTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[0]
    
    var b = a + 5
    var c = b + 4
    var d = c + 3
    var e = d + 2
    var f = e + 5
    var g = f + 1
    var h = g + 5

    if(a.len != 0 || b.len != 1 || c.len != 2 || d.len != 3 || e.len != 4 || f.len != 5 || g.len != 6 || h.len != 7)
        return 0

    if(h[1] != 5 || h[2] != 4)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListConcatSingleItemWithOperatorTest1()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a[0]
    
    var b = a + 5

    if(a.len != 0 || b.len != 1)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListExceptSingleItemWithOperatorTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/h = list(5, 4, 3, 2, 5, 1, 5)
    
    var g = h - 5
    var f = g - 1
    var e = f - 5
    var d = e - 2
    var c = d - 3
    var b = c - 4
    var a = b - 5

    if(a.len != 0 || b.len != 1 || c.len != 2 || d.len != 3 || e.len != 4 || f.len != 5 || g.len != 6 || h.len != 7)
        return 0

    if(f[1] != 5 || f[2] != 4 || f[5] != 5)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListAddListTest()
    {
        var dmlCode =
            @"
/proc/test0()
    var/list/a = list(5)
    
    a += list(4, 3, 2, 5, 1, 5)

    if(a[1] + a[2] + a[3] + a[4] + a[5] + a[6] + a[7] != 25)
        return 0

    if(a[1] != 5 || a[2] != 4 || a[5] != 5)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListRemoveListTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a = list(5, 4, 3, 2, 5, 1, 5)
    
    a -= list(5, 5)

    if(a[1] + a[2] + a[3] + a[4] + a[5] != 15)
        return 0

    if(a[1] != 5)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListAddListOfListsTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a = list()
    
    a += list(list(1,2,3), list(4,5,6))

    if(a.len != 2)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }

    [Test]
    public void ListFindTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a = list(1,2,3,4,5,6,1,2,3,4,5,6,9,9,9)

    if(a.Find(10) != 0)
        return 0

    if(a.Find(3) != 3)
        return 0

    if(a.Find(3, 3) != 3)
        return 0

    if(a.Find(3, 4) != 9)
        return 0

    if(a.Find(3, 4, 10) != 9)
        return 0

    if(a.Find(3, 4, 9) != 0)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListCopyTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a = list(1,2,3,4,5,6,7,8,9)

    var w = a.Copy()
    
    if(w.len != a.len)
        return 0

    a -= a.Copy(2, 7)
    
    if(a.len != 4)
        return 0

    if(w.len != 9)
        return 0

    if(a[1] != 1 || a[2] != 7)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListCutTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"
/proc/test0()
    var/list/a = list(1,2,3,4,5,6,7,8,9)

    var w = a.Copy()
    w.Cut()
    
    if(w.len != 0)
        return 0

    var c = a.Copy()
    c.Cut(3)

    if(c.len != 2 || c[1] != 1 || c[2] != 2)
        return 0

    a.Cut(2, 7)
    
    if(a.len != 4)
        return 0

    if(a[1] != 1 || a[2] != 7)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void ListNewListTest()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */

        var dmlCode =
            @"

/mob/bob
    var w = 25

/mob/tod
    var w = 10

/proc/test0()
    var/list/a = newlist(/mob/tod, /mob/bob)
    
    if(a[1].w + a[2].w != 35)
        return 0

    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        Assert.IsTrue((int)system.Global.ExecProc("test0").CompleteOrException() == 1);
    }


    [Test]
    public void TestExclusiveHolderList()
    {
        /*
         * Removal: The item is removed. If the same value exists twice, the one at the end of the list is removed first.
         */
        var assembly = MsBuildDmlCompiler.Compile("");
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var exclusiveListA = system.CreateDatum("/list/exclusive");
        var exclusiveListB = system.CreateDatum("/list/exclusive");

        var testMob0 = system.CreateAtomic("/mob");
        var testMob1 = system.CreateAtomic("/mob");

        exclusiveListA.ExecProc("Add", testMob0).CompleteOrException();

        Assert.IsTrue((int)exclusiveListA["len"] == 1);

        exclusiveListA.ExecProc("Add", testMob1).CompleteOrException();
        Assert.IsTrue((int)exclusiveListA["len"] == 2);

        exclusiveListB.ExecProc("Add", testMob0).CompleteOrException();
        Assert.IsTrue((int)exclusiveListA["len"] == 1);
        Assert.IsTrue((int)exclusiveListB["len"] == 1);


        exclusiveListB.ExecProc("Add", testMob1).CompleteOrException();
        Assert.IsTrue((int)exclusiveListA["len"] == 0);
        Assert.IsTrue((int)exclusiveListB["len"] == 2);
    }
}