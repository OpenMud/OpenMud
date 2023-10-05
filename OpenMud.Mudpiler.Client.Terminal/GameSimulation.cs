using System.Reflection;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Client.Terminal.Systems;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Core.Systems;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using SadConsole.UI.Controls;

namespace OpenMud.Mudpiler.Client.Terminal;

public delegate void WorldEcho(string message);

public delegate void EntityEcho(string instanceId, string name, string message);

public class GameSimulation
{
    private readonly ISystem<float> _renderSystem;
    private readonly ISystem<float> _system;
    private readonly World _world;

    private readonly IMudEntityBuilder entityBuilder = new BaseEntityBuilder();
    private readonly LogicDirectory logicDirectory = new();

    private readonly List<GoRogueSenseAdapter> SenseAdapters = new();

    private readonly MudEnvironment environment;

    public GameSimulation(IMudSceneBuilder builder, Assembly sourceAssembly, DrawingArea worldRenderTarget)
    {
        var entityBuilder = new BaseEntityBuilder();
        var scheduler = new TimeTaskScheduler();
        _world = new World();
        var densityAdapter = new GoRogueDensityAdapter(_world, builder.Bounds.Width, builder.Bounds.Height);

        var walkabilityAdapter = new GoRogueWalkabilityAdapter(densityAdapter);
        var visibilitySolver = new EntityVisibilitySolver(logicDirectory, densityAdapter);

        environment = MudEnvironment.Create(sourceAssembly,
            new BaseDmlFramework(_world, logicDirectory, scheduler, visibilitySolver));

        SenseAdapters.Add(densityAdapter);


        _renderSystem = new WorldTerminalRenderSystem(visibilitySolver, _world, worldRenderTarget);

        _system = new SequentialSystem<float>(
            new InteractionSystem(_world, visibilitySolver, logicDirectory, environment.Wrap),
            new UniqueIdentifierSystem(_world),
            new LogicCreationSystem(_world, logicDirectory, entityBuilder, environment),
            new LogicExecutorSystem(_world, logicDirectory),
            new VerbDiscoverySystem(_world, logicDirectory),
            new TerminalAnimatorSystem(_world),
            new MovementSystem(_world, logicDirectory),
            new LogicPropertiesPropagator(_world, logicDirectory),
            new TerminalAnimationBuilderSystem(_world, logicDirectory),
            new EntityVisualContextCacheSystem(_world, logicDirectory),
            new CommandDiscoverySystem(_world, visibilitySolver),
            new CommandDispatcherService(_world),
            new PathFindingSystem(_world, walkabilityAdapter),
            new ActionSystem<float>(deltaTime => scheduler.Update(deltaTime)),
            new GameFlowSystem(_world, logicDirectory),
            new EntityVisionSystem(_world, visibilitySolver)
        );

        _world.Subscribe<WorldEchoMessage>(On);
        _world.Subscribe<EntityEchoMessage>(On);

        builder.Build(_world);
    }

    public event WorldEcho OnWorldEcho;
    public event EntityEcho OnEntityEcho;

    private void On(in EntityEchoMessage message)
    {
        var logicId = message.Id;

        var e = _world.Where(
            e => e.Has<LogicIdentifierComponent>() && e.Get<LogicIdentifierComponent>().LogicInstanceId == logicId
        ).FirstOrDefault();

        var name = e.Get<IdentifierComponent>().Name;
        var eh = logicDirectory[logicId];

        if (OnEntityEcho != null)
            OnEntityEcho(name, eh["name"], message.Message);
    }

    private void On(in WorldEchoMessage message)
    {
        OnWorldEcho(message.Message);
    }

    public void Update(float deltaTimeSeconds)
    {
        _system.Update(deltaTimeSeconds);

        foreach (var a in SenseAdapters)
            a.ClearCache();
    }

    public void Render(float deltaTimeSeconds)
    {
        _renderSystem.Update(deltaTimeSeconds);
    }

    internal void Slide(string subject, int deltaX, int deltaY)
    {
        var cost = MovementCost.Compute(deltaX, deltaY);

        bool nameMatches(in IdentifierComponent i)
        {
            return i.Name == subject;
        }

        var entity = _world.GetEntities().With<IdentifierComponent>(nameMatches).AsEnumerable().Single();

        if (deltaX == 0 && deltaY == 0)
            entity.Remove<SlideComponent>();
        else
            entity.Set(new SlideComponent(deltaX, deltaY, cost));
    }

    internal Entity GetEntity(string subject)
    {
        bool nameMatches(in IdentifierComponent i)
        {
            return i.Name == subject;
        }

        var entity = _world.GetEntities().With<IdentifierComponent>(nameMatches).AsEnumerable().SingleOrDefault();

        return entity;
    }

    internal void DispatchCommand(string entity, string command)
    {
        var e = _world.CreateEntity();

        var cmd = command.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (cmd.Length == 0)
            return;

        var verb = cmd[0];
        var operands = cmd.Skip(1).ToArray();

        e.Set(new ExecuteCommandComponent(entity, null, verb, operands));
    }

    internal Entity CreatePlayer(string name)
    {
        var entity = _world.CreateEntity();

        var mobType = environment.World.Unwrap<GameWorld>().mob.Get<Type>();
        entityBuilder.CreateAtomic(entity, environment.TypeSolver.LookupName(mobType), name);

        return entity;
    }
}