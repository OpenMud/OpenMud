﻿using System.Reflection;
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
}