﻿using System.Collections.Immutable;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using OpenMud.Mudpiler.Compiler.Project.Project;
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
#else
norld
#endif
";

        var r = Preprocessor.Preprocess(
            "testfile.dml",
            ".",
            Enumerable.Empty<string>(),
            testCode,
            NullResourceResolver,
            processImport,
            out _);
        Assert.IsTrue(r.Trim() == "world");
    }


    [Test]
    public void TestIfDefElse()
    {
        var testCode =
            @"

#define y

#ifdef x
hello
#else
sello
#endif

#ifdef y
world
#endif
";

        var r = Preprocessor.Preprocess(
            "testfile.dml",
            ".",
            Enumerable.Empty<string>(),
            testCode,
            NullResourceResolver,
            processImport,
            out _);

        var result = r.Trim().Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.IsTrue(result.Length == 2);
        Assert.IsTrue(result[0] == "sello");
        Assert.IsTrue(result[1] == "world");
    }


    [Test]
    public void TestIfDefIndent()
    {
        var testCode =
            @"
/proc/test()
    if(0 == 0)
        var x = 11
        #ifdef xyz
        x = x + 1
        #endif
        return x
    return 8
";

        var r = Preprocessor.Preprocess(
            "testfile.dml",
            ".",
            Enumerable.Empty<string>(),
            testCode,
            NullResourceResolver,
            processImport,
            out _);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == 11);
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
        var r = Preprocessor.Preprocess(
            "testFile.dml",
            ".",
            Enumerable.Empty<string>(),
            testCode,
            NullResourceResolver,
            processImport,
            out _);
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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);
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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "hello world");
    }



    [Test]
    public void TestFileImportDoesntTriggerWhenExcluded()
    {
        var testCode =
            @"
#ifdef xyx
#include ""hello.dm""
not
#endif
hello world
";

        (IImmutableDictionary<string, MacroDefinition> macros, IImmutableSourceFileDocument importBody) resolveImport(IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib, string fileName)
        {
            throw new Exception("Should not be eexecuting any import directives.");
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, resolveImport,
            out var resultant);
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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);

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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);
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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);
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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);
        var words = string.Join(" ",
            r.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(words == "1 + 2 + 3");
    }

    [Test]
    public void TestMacroTemplating2()
    {
        var testCode =
            @"
#define DEBUG_OUT(x) addtext(""DBGOUT: "", x)

/proc/test()
    return DEBUG_OUT("":( forceUpdate"")
";
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == "DBGOUT: :( forceUpdate");
    }

    [Test]
    public void TestMacroTemplating3()
    {
        var testCode =
            @"
#define C * 10 + 2

/proc/test()
    return 2 C
";
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == 22);
    }

    [Test]
    public void TestMacroTemplating4()
    {
        var testCode =
            @"
#define C -1

/proc/test()
    return -C
";
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == 1);
    }

    [Test]
    public void TestMacroTemplating5()
    {
        var testCode =
            @"
#define C 1-

/proc/test()
    return C-1
";
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == 2);
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
        var r = Preprocessor.Preprocess("testFile.dml", ".", Enumerable.Empty<string>(), testCode, NullResourceResolver, processImport,
            out var resultant);
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
    public void ResourceWithEscapeSequence()
    {
        var testCode =
            @"
turf/door/open
	icon = 'sound/vox/wizard\'s.ogg'
";

        string resolve(List<string> possible, string rsrc)
        {
            Assert.IsTrue(possible.Count == 0);

            return rsrc;
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var name = "/turf/door/open";
        var instance = system.CreateAtomic(name);

        var icon = (string)instance["icon"];

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(icon == @"sound\vox\wizard's.ogg");
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

        Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        //This is not a useless test. The assertion is that resolve is never called.
        //Do NOT remove this test.
    }


    [Test]
    public void MultiLineMacroTest()
    {
        var testCode =
            @"
#define SAY_A_LOT(message) \
message \
message \
message \
message

SAY_A_LOT(""Test"")

";

        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        Assert.IsTrue(r.Contains(@"""Test"" ""Test"" ""Test"" ""Test"""));
    }

    [Test]
    public void StringProcessingTest()
    {
        var testCode = @"
/proc/test()
    world << ""<span style=\""color:red\"">Cannot create a HUD with no name![prob(5) ? "" It's not a horse!"" : null]</span>"" // c:
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        //Just testing that this doesn't cause a crash
    }

    [Test]
    public void TestMacroDefInString()
    {

        var testCode = @"
x = ""#define bob 10""
this should not be replaced bob
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        Assert.IsTrue(r.Contains(@"""#define bob 10"));
        Assert.IsTrue(r.Contains("this should not be replaced bob"));
    }



    [Test]
    public void TestStringCoalacing()
    {

        var testCode = @"
/proc/test()
    var x = 86
    return ""this is a \\[10 + 10] string with [10 + ""several sub [ x + 6 ]""] expressions\[\""this is not an expression]""
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(instance == @"this is a \20 string with 10several sub 92 expressions\[""this is not an expression]");
    }

    [Test]
    public void TestMultilineStringCoalacing()
    {

        var testCode = @"
/proc/test()
    return {""this is a
[10 + 10]
multiline
string
""}
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(instance == "this is a\r\n20\r\nmultiline\r\nstring\r\n");
    }

    [Test]
    public void TestMultilineStringWithNestedString()
    {

        var testCode = @"
/proc/test()
    return {""this is a
""multiline""
string
""}
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(instance == "this is a\r\n\"multiline\"\r\nstring\r\n");
    }

    [Test]
    public void TestMultilineStringWithNestedEscapedString()
    {

        var testCode = @"
/proc/test()
    return {""this is a
\""multiline\""
string
""}
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();

        //Path is normalized in preprocessor, so it should always come out looking like this...
        Assert.IsTrue(instance == "this is a\r\n\"multiline\"\r\nstring\r\n");
    }

    [Test]
    public void UsingMacroWithNameCollision()
    {
        var testCode = @"
#define isrestrictedz(z) ((z) == 2 || (z) == 4)
isrestrictedz(O.z)
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        Assert.IsTrue(r.Contains("((O.z) == 2 || (O.z) == 4)"));
    }

    [Test]
    public void LineDelimString()
    {
        var testCode = @"
/proc/test()
    return ""this is a string\
 and it is multiline""
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == "this is a string and it is multiline");
    }

    [Test]
    public void LineDelimString2()
    {
        var testCode = @"
/proc/test()
    return ""\
this is multiline""
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == "this is multiline");
    }

    [Test]
    public void MultiLineDelimString()
    {
        var testCode = @"
/proc/test()
    return {""this is a string\
 and it is multiline""}
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == "this is a string and it is multiline");
    }


    [Test]
    public void MultilineCommentPreserveIndentation()
    {
        var testCode = @"

/proc/test()
    var w = new/mob/tm()
    return w.test()

/mob/tm
    /proc/test()
        /* this is a comment
    and I am hoping that the identation
    is actually being
    preserved       .*/if(0 == 0)
            if(0 == 0)
                return 10
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (int)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == 10);
    }

    [Test]
    public void MultiLineDelimString2()
    {
        var testCode = @"
/proc/test()
    return {""\
this is multiline""}
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance == "this is multiline");
    }

    [Test]
    public void FormattedTextFromMacroTest()
    {
        var testCode = @"

#define TEST_FORMAT(x) {""My Test [x] Format""}
/proc/test()
    var/x = 10
    return TEST_FORMAT(x * 2)
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);
        var assembly = MsBuildDmlCompiler.Compile(r);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        var instance = (string)system.Global.ExecProc("test").CompleteOrException();
        Assert.IsTrue(instance.Trim() == "My Test 20 Format");
    }


    [Test]
    public void TestMacroArgumentIntegrityWithBraceAndComma()
    {
        var testCode = @"
#define neg(x) -x

/proc/test()
    return neg(sqrt(32, 5))
";
        string resolve(List<string> possible, string rsrc)
        {
            Assert.Fail("Should not be importing any resource...");
            return "";
        }

        var r = Preprocessor.Preprocess("testFile.dml", ".", testCode, resolve, processImport, EnvironmentConstants.BUILD_MACROS);

        Assert.IsTrue(r.Contains("-sqrt(32, 5)"));
    }

    private (IImmutableDictionary<string, MacroDefinition> macros, IImmutableSourceFileDocument importBody) processImport(
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