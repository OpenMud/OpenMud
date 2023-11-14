using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class VerbTests
{
    [Test]
    public void TestSimpleVerbDiscoveryOnSelf()
    {
        var dmlCode =
            @"
/mob/test
    verb/test_verb()
        return 20

    verb/test_named_verb()
        set name = ""break""
        return 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var asm = system.CreateAtomic("/mob/test");

        var verbs = asm.DiscoverVerbs(asm).ToHashSet();

        Assert.IsTrue(verbs.Count == 2);
        Assert.IsTrue(verbs.Contains("test_verb"));
        Assert.IsTrue(verbs.Contains("break"));

        Assert.IsTrue(20 == (int)asm.Interact(asm, "test_verb").CompleteOrException());
        Assert.IsTrue(10 == (int)asm.Interact(asm, "break").CompleteOrException());
    }


    [Test]
    public void TestVerbCategories()
    {
        var dmlCode =
            @"
/mob/test
    verb/testa_a()
        set category = ""a""
        return 20

    verb/testa_b()
        set category = ""a""
        return 10

    verb/testb_a()
        set category = ""b""
        return 20

    verb/testb_b()
        set category = ""b""
        return 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var asm = system.CreateAtomic("/mob/test");

        var verbs = asm.DiscoverVerbs(asm, "a").ToHashSet();

        Assert.IsTrue(verbs.Count == 2);
        Assert.IsTrue(verbs.Contains("testa_a"));
        Assert.IsTrue(verbs.Contains("testa_b"));

        verbs = asm.DiscoverVerbs(asm, "b").ToHashSet();
        Assert.IsTrue(verbs.Count == 2);
        Assert.IsTrue(verbs.Contains("testb_a"));
        Assert.IsTrue(verbs.Contains("testb_b"));

        var verbCategories = asm.DiscoverVerbCategories(asm).ToHashSet();
        Assert.IsTrue(verbCategories.Count == 2);
        Assert.IsTrue(verbCategories.Contains("a"));
        Assert.IsTrue(verbCategories.Contains("b"));
    }


    [Test]
    public void TestVerbSourcesTranslation()
    {
        var dmlCode =
            @"
/mob/test
    verb/testa0()
        set src = usr
        return 10

    verb/testa1()
        set src in usr
        return 10

    verb/testb0()
        set src in usr.contents
        return 10

    verb/testb1()
        set src = usr.contents
        return 10

    verb/testc0()
        set src in usr.group

    verb/testc1()
        set src = usr.group
        return 20

    verb/testd()
        set src = usr.loc
        return 10

    verb/teste0()
        set src = view(10)

    verb/teste1()
        set src in view(10)
        return 10

    verb/testf0()
        set src = oview(20)

    verb/testf1()
        set src in oview(20)
        return 10
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var asm = system.CreateAtomic("/mob/test");
        var verbs = asm.DiscoverVerbs(asm).ToHashSet();

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testa0").Source == SourceType.User);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testa1").Source == SourceType.UserContents);

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testb0").Source == SourceType.UserContents);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testb1").Source == SourceType.UserContents);

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testc0").Source == SourceType.UserGroup);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testc1").Source == SourceType.UserGroup);

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testd").Source == SourceType.UserLoc);

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("teste0").Source == SourceType.View &&
                      asm.DiscoverOwnedVerbSource("teste0").Argument == 10);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("teste1").Source == SourceType.View &&
                      asm.DiscoverOwnedVerbSource("teste1").Argument == 10);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testf0").Source == SourceType.OView &&
                      asm.DiscoverOwnedVerbSource("testf0").Argument == 20);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testf1").Source == SourceType.OView &&
                      asm.DiscoverOwnedVerbSource("testf1").Argument == 20);
    }


    [Test]
    public void TestVerbSourcesIn_vs_srcEquals_usr()
    {
        var dmlCode =
            @"
/mob/test
    verb/testa()
        set src = usr
        return 20

    verb/testb()
        set src in usr
        return 10

    verb/testc()
        set src in usr.contents
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var asm = system.CreateAtomic("/mob/test");
        var verbs = asm.DiscoverVerbs(asm).ToHashSet();

        Assert.IsTrue(verbs.Count == 3);
        Assert.IsTrue(verbs.Contains("testa"));
        Assert.IsTrue(verbs.Contains("testb"));
        Assert.IsTrue(verbs.Contains("testc"));

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testa").Source == SourceType.User);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testb").Source == SourceType.UserContents);
        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testc").Source == SourceType.UserContents);
    }


    [Test]
    public void TestVerbInheritance()
    {
        var dmlCode =
            @"
/mob/verb/testa()
    set src = usr
    return 20

/mob/test
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var asm = system.CreateAtomic("/mob/test");

        var verbs = asm.DiscoverVerbs(asm).ToHashSet();

        Assert.IsTrue(verbs.Count == 1);
        Assert.IsTrue(verbs.Contains("testa"));
        Assert.IsTrue(20 == (int)asm.Interact(asm, "testa").CompleteOrException());

        Assert.IsTrue(asm.DiscoverOwnedVerbSource("testa").Source == SourceType.User);
    }


    [Test]
    public void TestVerbOverride()
    {
        var dmlCode =
            @"
/mob/verb/tverb()
    set src = usr
    set name = ""Test Verb Name""
    return 20

/mob/testa/tverb()
    return 30

/mob/testa/testb/tverb()
    return 40

/mob/testa/testb/testc/tverb()
    set name = ""Override Verb Name""
    return 50
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var testMob = system.CreateAtomic("/mob");
        var testa = system.CreateAtomic("/mob/testa");
        var testb = system.CreateAtomic("/mob/testa/testb");
        var testc = system.CreateAtomic("/mob/testa/testb/testc");

        var mobVerbs = testMob.DiscoverVerbs(testMob).ToHashSet();
        var testaVerbs = testa.DiscoverVerbs(testa).ToHashSet();
        var testbVerbs = testb.DiscoverVerbs(testb).ToHashSet();
        var testcVerbs = testc.DiscoverVerbs(testc).ToHashSet();

        Assert.IsTrue(mobVerbs.Count == 1);
        Assert.IsTrue(testaVerbs.Count == 1);
        Assert.IsTrue(testbVerbs.Count == 1);
        Assert.IsTrue(testcVerbs.Count == 1);

        Assert.IsTrue(mobVerbs.Contains("Test Verb Name"));
        Assert.IsTrue(testaVerbs.Contains("Test Verb Name"));
        Assert.IsTrue(testbVerbs.Contains("Test Verb Name"));
        Assert.IsTrue(testcVerbs.Contains("Override Verb Name"));

        Assert.IsTrue((int)testMob.Interact(testMob, "Test Verb Name").CompleteOrException() == 20);
        Assert.IsTrue((int)testa.Interact(testa, "Test Verb Name").CompleteOrException() == 30);
        Assert.IsTrue((int)testb.Interact(testb, "Test Verb Name").CompleteOrException() == 40);
        Assert.IsTrue((int)testc.Interact(testc, "Override Verb Name").CompleteOrException() == 50);
    }


    [Test]
    public void TestAddVerbWithNew()
    {
        var dmlCode =
            @"
/mob/testa
    proc
        exampleverb()
            return 15

/mob/testb
    test_method()
        new/mob/testa/proc/exampleverb(src, ""renamedverb"", ""example description"")

    test_unmethod()
        verbs -= /mob/testa/exampleverb
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var mob = system.CreateAtomic("/mob/testb");

        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 0);

        mob.ExecProc("test_method").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 1);

        mob.ExecProc("test_method").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 1);
        Assert.IsTrue(mob.DiscoverVerbMetadata(mob).Single().description == "example description");

        Assert.IsTrue(15 == (int)mob.Interact(mob, "renamedverb").CompleteOrException());

        mob.ExecProc("test_unmethod").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 1);
        mob.ExecProc("test_unmethod").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 0);
    }

    [Test]
    public void TestDynamicVerbs()
    {
        var dmlCode =
            @"
/mob/testa
    exampleverb()
        return 15

/mob/testb
    test_method()
        verbs += /mob/testa/exampleverb

    test_unmethod()
        verbs -= /mob/testa/exampleverb
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var mob = system.CreateAtomic("/mob/testb");

        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 0);

        mob.ExecProc("test_method").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 1);

        mob.ExecProc("test_method").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 1);

        Assert.IsTrue(15 == (int)mob.Interact(mob, "exampleverb").CompleteOrException());

        mob.ExecProc("test_unmethod").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 1);
        mob.ExecProc("test_unmethod").CompleteOrException();
        Assert.IsTrue(mob.DiscoverVerbs(mob).Count() == 0);
    }

    [Test]
    public void TestVerbDescSetting()
    {
        var dmlCode =
            @"
/mob/test
    verb/testaa()
        set desc = ""an example description""
        return 20
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        var asm = system.CreateAtomic("/mob/test");

        var verbs = asm.DiscoverVerbMetadata(asm).Single();

        Assert.IsTrue(verbs.description == "an example description");
    }

    [Test]
    public void VerbArgumentAsNullTest()
    {
        var dmlCode =
            @"
/mob/test
    verb/testaa(a as null)
        return
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/test");
        var verb = mob.DiscoverVerbMetadata(mob).Where(v => v.verbName == "testaa").Single();

        var md = mob.Unwrap<Datum>().EnumerateProcs().Where(p => p.Name == "testaa").Single();
        Assert.IsTrue(md.Attributes.Where(a => a is Verb).Single() != null);
        Assert.IsTrue(md.Attributes.Where(a => a is ArgAsConstraint).Cast<ArgAsConstraint>().Single().Expected ==
                      ArgAs.Null);
    }

    [Test]
    public void TestOverrideVerbSources()
    {
        var dmlCode =
            @"
/mob
    verb/testaa()
        set src in oview(1)

/mob/test
    verb/testaa()
        set src in oview(25)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var mob = system.CreateAtomic("/mob/test");
        var verb = mob.DiscoverVerbMetadata(mob).Where(v => v.verbName == "testaa").Single();

        var md = mob.Unwrap<Datum>().EnumerateProcs().Where(p => p.Name == "testaa").Single();
        Assert.IsTrue(md.Attributes.Where(a => a is Verb).Single() != null);

        var srcConstraint = md.Attributes.Where(a => a is VerbSrc).Cast<VerbSrc>().Single();

        Assert.IsTrue(srcConstraint.Source == SourceType.OView && srcConstraint.Argument == 25);
    }
}