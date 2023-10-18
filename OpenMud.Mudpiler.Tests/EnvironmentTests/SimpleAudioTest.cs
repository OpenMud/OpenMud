using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.Framework.Datums;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests;

public class SimpleAudioTest
{
    [Test]
    public void SoundProcTest()
    {
        var dmlCode =
            @"
/proc/createSoundTest()
    return sound('hello.wav')
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        DatumHandle r = (DatumHandle)system.Global.ExecProc("createSoundTest").CompleteOrException();

        var info = r.Unwrap<SoundInfo>();

        Assert.That(info.channel.Get<int>(), Is.EqualTo(0));
        Assert.That(info.file.Get<string>(), Is.EqualTo("hello.wav"));
        Assert.That(info.repeat.Get<int>(), Is.EqualTo(0));
        Assert.That(info.volume.Get<int>(), Is.EqualTo(100));
    }

    [Test]
    public void SoundProcTest2()
    {
        var dmlCode =
            @"
/proc/createSoundTest()
    return sound('hello2.wav', channel = 2, volume = 75, repeat = 1)

/proc/createSoundTest0()
    return sound('hello2.wav', 1, channel = 2, volume = 75)
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());


        DatumHandle r = (DatumHandle)system.Global.ExecProc("createSoundTest").CompleteOrException();
        DatumHandle r1 = (DatumHandle)system.Global.ExecProc("createSoundTest0").CompleteOrException();

        var info = r.Unwrap<SoundInfo>();
        var info1 = r1.Unwrap<SoundInfo>();

        Assert.That(info.channel.Get<int>(), Is.EqualTo(2));
        Assert.That(info.file.Get<string>(), Is.EqualTo("hello2.wav"));
        Assert.That(info.repeat.Get<int>(), Is.EqualTo(1));
        Assert.That(info.volume.Get<int>(), Is.EqualTo(75));

        Assert.That(info1.channel.Get<int>(), Is.EqualTo(2));
        Assert.That(info1.file.Get<string>(), Is.EqualTo("hello2.wav"));
        Assert.That(info1.repeat.Get<int>(), Is.EqualTo(1));
        Assert.That(info1.volume.Get<int>(), Is.EqualTo(75));
    }

    [Test]
    public void SoundDatumTest0()
    {
        var dmlCode =
            @"
/proc/createSoundTest()
    var/sound/s = new('hello3.wav')
    return s
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        DatumHandle r = (DatumHandle)system.Global.ExecProc("createSoundTest").CompleteOrException();

        var info = r.Unwrap<SoundInfo>();

        Assert.That(info.channel.Get<int>(), Is.EqualTo(0));
        Assert.That(info.file.Get<string>(), Is.EqualTo("hello3.wav"));
        Assert.That(info.repeat.Get<int>(), Is.EqualTo(0));
        Assert.That(info.volume.Get<int>(), Is.EqualTo(100));
    }


    [Test]
    public void SoundDatumPropertiesTest0()
    {
        var dmlCode =
            @"
/proc/createSoundTest()
    var/sound/s = new('hello3.wav')
    s.repeat = 1
    s.volume = 50
    s.channel = 8
    return s
";
        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
        var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());

        DatumHandle r = (DatumHandle)system.Global.ExecProc("createSoundTest").CompleteOrException();

        var info = r.Unwrap<SoundInfo>();

        Assert.That(info.channel.Get<int>(), Is.EqualTo(8));
        Assert.That(info.file.Get<string>(), Is.EqualTo("hello3.wav"));
        Assert.That(info.repeat.Get<int>(), Is.EqualTo(1));
        Assert.That(info.volume.Get<int>(), Is.EqualTo(50));
    }

}