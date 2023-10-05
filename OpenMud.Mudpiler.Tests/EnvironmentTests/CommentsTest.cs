using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class CommentsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TearDown]
    public void Teardown()
    {
    }

    [Test]
    public void MultiLineComment()
    {
        var dmlCodeCommented =
            @"
/*
proc/test_commented()
    return 1
*/
var/num/test_input = 10
";


        var dmlCodeUnCommented =
            @"
proc/test_commented()
    return 1

var/num/test_input = 10
";
        var systemCommented = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeCommented)),
            new BaseDmlFramework());
        Assert.IsTrue(!systemCommented.Global.HasProc("test_commented"));

        var systemUncommented = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeUnCommented)),
            new BaseDmlFramework());
        Assert.IsTrue(systemUncommented.Global.HasProc("test_commented"));
    }

    //This isn't how multiline comments work in literally any other language by the way, since
    //we actually need to parse the contents a comment now which isn't how comments are supposed to work in the real world
    //but I guess that is how they work in Dream Maker???
    [Test]
    public void MultiLineNestedComments()
    {
        var dmlCodeCommented =
            @"

proc/test_commented0()
    return 1
/*
	/* yeah this is another nested comment which is supposed to work? */
	Hmm
*/

proc/test_commented1()
    return 1
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCodeCommented);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
        Assert.IsTrue(system.Global.HasProc("test_commented0"));
        Assert.IsTrue(system.Global.HasProc("test_commented1"));
    }

    [Test]
    public void InlineMultilineComments()
    {
        var dmlCodeCommented =
            @"

proc/test_commented0()
/*
	Hmm
*/
    return 1
";
        var system = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeCommented)),
            new BaseDmlFramework());
        Assert.IsTrue(system.Global.HasProc("test_commented0"));
    }


    [Test]
    public void SingleLineComment()
    {
        var dmlCodeCommented =
            @"

//proc/test_commented()
//    return 1

var/num/test_input = 10
";


        var dmlCodeUnCommented =
            @"
proc/test_commented()
    return 1

var/num/test_input = 10
";

        var systemCommented = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeCommented)),
            new BaseDmlFramework());
        Assert.IsTrue(!systemCommented.Global.HasProc("test_commented"));

        var systemUncommented = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeUnCommented)),
            new BaseDmlFramework());
        Assert.IsTrue(systemUncommented.Global.HasProc("test_commented"));
    }


    [Test]
    public void SingleLineCommentArt()
    {
        var dmlCodeCommented =
            @"
///////////////////////////////////////////////////////////////////////////////////////////////////
// PLAYER //
////////////
//proc/test_commented()
//    return 1

var/num/test_input = 10
";


        var systemCommented = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeCommented)),
            new BaseDmlFramework());
        Assert.IsTrue(!systemCommented.Global.HasProc("test_commented"));
    }


    [Test]
    public void CommentWithStatement()
    {
        var dmlCodeCommented =
            @"
turf/testturf            //what? no corona?
    name = ""palm tree""
    icon = 'palm 1.dmi'
    icon_state = ""3""      //this is the frame of the icon with the trunk, we'll start here
";


        var systemCommented = MudEnvironment.Create(Assembly.LoadFile(MsBuildDmlCompiler.Compile(dmlCodeCommented)),
            new BaseDmlFramework());
        var test = systemCommented.CreateAtomic("/turf/testturf");
        Assert.IsTrue(test["icon_state"] == "3");
    }
}