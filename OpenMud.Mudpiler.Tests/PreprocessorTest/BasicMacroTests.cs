using System.Collections.Immutable;
using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.PreprocessorTest;

public class BasicMacroTests
{
    private readonly Dictionary<string, string> importableFiles = new();
    private readonly Dictionary<string, string> importableLibFiles = new();

    [TearDown]
    public void TearDown()
    {
        importableFiles.Clear();
        importableLibFiles.Clear();
    }

    [SetUp]
    public void Setup()
    {
        importableFiles.Clear();
        importableLibFiles.Clear();
    }

    [Test]
    public void TestIfDef()
    {
        var testCode =
            @"

#define y

#ifdef x
hello
#endif

#ifdef y
world
#endif
";

        var r = Preprocessor.PreprocessAsDocument(
            "testfile.dml",
            ".",
            Enumerable.Empty<string>(),
            testCode, 
            NullResourceResolver,
            processImport,
            out _).AsPlainText(false);
        Assert.IsTrue(r.Trim() == "world");
    }

    [Test]
    public void TestMacroAsVarInitViaImport()
    {
        var testCode =
            @"
#include ""testfile.dm""

";
        importableFiles["testfile.dm"] =
            @"
/mob/test
    var
        testvar = TEST_VAR_VALUE
";
        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, NullResourceResolver, processImport,
            new Dictionary<string, MacroDefinition>
            {
                { "TEST_VAR_VALUE", new MacroDefinition("\"test_value\"") }
            }.ToImmutableDictionary());

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue(instance["testvar"] == "test_value");
    }


    [Test]
    public void TestMacroAsVarInit()
    {
        var testCode =
            @"
/mob/test
    var
        testvar = TEST_VAR_VALUE
";
        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, NullResourceResolver, processImport,
            new Dictionary<string, MacroDefinition>
            {
                { "TEST_VAR_VALUE", new MacroDefinition("\"test_value\"") }
            }.ToImmutableDictionary());

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue(instance["testvar"] == "test_value");
    }


    [Test]
    public void TestMacroReplaceBetweenSingleQuoteComment()
    {
        var testCode =
            @"
//this sentence isn't a string but has the single quote char in it.
/mob/test
    var
        map_format = TOPDOWN_MAP	// This is another string with a single quote'

";
        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, NullResourceResolver, processImport,
            new Dictionary<string, MacroDefinition>
            {
                { "TOPDOWN_MAP", new MacroDefinition("\"test_value\"") }
            }.ToImmutableDictionary());

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue(instance["map_format"] == "test_value");
    }


    [Test]
    public void TestMacroReplaceBetweenDoubleQuoteComment()
    {
        var testCode =
            @"
//this sentence isn""t a string but has the single quote char in it.
/mob/test
    var
        map_format = TOPDOWN_MAP	// This is another string with a single quote""

";
        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, NullResourceResolver, processImport,
            new Dictionary<string, MacroDefinition>
            {
                { "TOPDOWN_MAP", new MacroDefinition("\"test_value\"") }
            }.ToImmutableDictionary());

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue(instance["map_format"] == "test_value");
    }


    [Test]
    public void PreprocessorDoesntCommentStringsTest()
    {
        var testCode =
            @"
/mob/test
    var
        map_format = ""hello//world""

";
        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, NullResourceResolver, processImport);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue(instance["map_format"] == "hello//world");
    }

    [Test]
    public void TestIfnDef()
    {
        var testCode =
            @"

#define y

#ifndef x
hello
#endif

#ifndef y
world
#endif
";
        var r = Preprocessor.PreprocessAsDocument(
            "testFile.dml",
            ".",
            Enumerable.Empty<string>(),
            testCode,
            NullResourceResolver,
            processImport,
            out _)
            .AsPlainText(false);
        Assert.IsTrue(r.Trim() == "hello");
    }


    [Test]
    public void TestOutputDefinitions()
    {
        var testCode =
            @"
#define DEBUG
hello
";
        var r = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant)
            .AsPlainText(false);
        Assert.IsTrue(r.Trim() == "hello");
        Assert.IsTrue(resultant.ContainsKey("DEBUG"));
    }

    [Test]
    public void TestFileImport()
    {
        var testCode =
            @"
#include ""hello.dm""
world
";
        importableFiles.Add("hello.dm", "hello");
        var r = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant)
            .AsPlainText(false);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "hello world");
    }

    [Test]
    public void TestLibFileImport()
    {
        var testCode =
            @"
#include <hello.dm>
world
";
        importableLibFiles.Add("hello.dm", "hello");
        var rDoc = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);

        var r = rDoc.AsPlainText(false);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "hello world");
    }

    [Test]
    public void TestFileImportImpactsMacros()
    {
        var testCode =
            @"
#define test
#include ""testfile.dm""

#ifdef test
world
#endif
";
        importableFiles["testfile.dm"] = @"
#undef test
hello";
        var r = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant)
            .AsPlainText(false);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "hello");
    }


    [Test]
    public void TestUndef()
    {
        var testCode =
            @"
#define test

#ifdef test
hello
#endif

#undef test

#ifdef test
world
#endif
";
        var r = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant)
            .AsPlainText(false);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "hello");
    }


    [Test]
    public void TestMacroTemplating()
    {
        var testCode =
            @"
#define test(x, y, z) x + y + z

test(1,2,3)
";
        var r = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant)
            .AsPlainText(false);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "1 + 2 + 3");
    }

    [Test]
    public void TestMacroDoesntImpactStrings()
    {
        var testCode =
            @"
#define test(x, y, z) x + y + z

test(1,2,3)
""test(1,2,3) test test test""
";
        var r = Preprocessor.PreprocessAsDocument("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant)
            .AsPlainText(false);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "1 + 2 + 3 \"test(1,2,3) test test test\"");
    }


    [Test]
    public void TrueFalseMacroTest()
    {
        var testCode =
            @"

/mob
    var
        testTrue
        testFalse

/mob/test
    name = ""table""
    testTrue = TRUE
    testFalse = FALSE

";
        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, NullResourceResolver, processImport,
            EnvironmentConstants.BUILD_MACROS);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        Assert.IsTrue((int)instance["testTrue"] == 1);
        Assert.IsTrue((int)instance["testFalse"] == 0);
    }

    [Test]
    public void FILEDIR_MacroTest2()
    {
        var testCode =
            @"
#define FILE_DIR icons
#define FILE_DIR icons2
/* The door is handled here using two kinds of turfs, one open and one closed.
   The door could also be treated as one obj or turf with different states, so this is
   just to demonstrate how you can replace turfs at framework. When the open door is
   closed, the turf/door/open object is replaced by a turf/door/closed object. */
turf/door
	name = ""door""

turf/door/open
	icon = 'open_door.dmi'
";

        string resolve(List<string> possible, string rsrc)
        {
            Assert.IsTrue(possible.Count == 2);

            //Comparing paths is a whole can of worms...
            Assert.IsTrue(possible[0].EndsWith("icons"));
            Assert.IsTrue(possible[1].EndsWith("icons2"));

            return Path.Combine(possible[1], rsrc);
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/turf/door/open";
        var instance = system.CreateAtomic(name);

        var icon = (string)instance["icon"];

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(icon == "icons2\\open_door.dmi");
    }

    [Test]
    public void FILDIR_MacroTest()
    {
        var testCode =
            @"
#define FILE_DIR icons
#define FILE_DIR icons2

/mob/test
    name = ""table""
    icon = 'table.dmi'

";

        string resolve(List<string> possible, string rsrc)
        {
            Assert.IsTrue(possible.Count == 2);

            //Comparing paths is a whole can of worms...
            Assert.IsTrue(possible[0].EndsWith("icons"));
            Assert.IsTrue(possible[1].EndsWith("icons2"));

            return Path.Combine(possible[1], rsrc);
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/mob/test";
        var instance = system.CreateAtomic(name);

        var icon = (string)instance["icon"];

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(icon == "icons2\\table.dmi");
    }


    [Test]
    public void FILDIR_MacroInCommentTest()
    {
        var testCode =
            @"
//'this is not a file import'

/*
   and 'neither is this'
*/

//'this is not a file import'
/mob/test
    name = ""table""

";

        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess( "testFile.dml",".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
    }

    private (IImmutableDictionary<string, MacroDefinition> macros, SourceFileDocument importBody) processImport(
        IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib,
        string fileName)
    {
        var contents = isLib ? importableLibFiles[fileName] : importableFiles[fileName];
        var r = Preprocessor.PreprocessAsDocument(fileName, ".", resourceDirectories, contents, NullResourceResolver, processImport,
            out var newMacros, dict);

        return (newMacros, r);
    }

    private static string NullResourceResolver(List<string> possible, string name)
    {
        throw new NotImplementedException();
    }
}