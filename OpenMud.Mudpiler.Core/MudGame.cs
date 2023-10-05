using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Scene;

namespace OpenMud.Mudpiler.Core;

public readonly record struct Breakable;

public class MudGame
{
    private readonly ISystem<float> _system;
    private readonly World _world;
    private TextWriter output;

    public MudGame(IMudSceneBuilder builder, TextWriter output)
    {
        _world = new World();
        _system = new SequentialSystem<float>(
            //new InteractionSystem(_world)
        );

        builder.Build(_world);

        _world.Subscribe<WorldEchoMessage>(On);
    }

    private void On(in WorldEchoMessage message)
    {
        output.WriteLine(message);
    }


    public void Update(float deltaTimeMs)
    {
        _system.Update(deltaTimeMs);
    }
}