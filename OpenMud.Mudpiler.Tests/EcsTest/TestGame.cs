using System.Reflection;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Core.Systems;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Tests.EcsTest;

internal class TestGame
{
    private readonly ISystem<float> _system;
    private readonly World _world;

    public readonly MudEnvironment Environment;

    private readonly LogicDirectory logicDirectory = new();
    private readonly List<GoRogueSenseAdapter> SenseAdapters = new();

    public TestGame(IMudSceneBuilder builder, Assembly sourceAssembly)
    {
        var scheduler = new TimeTaskScheduler();
        var entityBuilder = new BaseEntityBuilder();
        _world = new World();

        var densityAdapter = new GoRogueDensityAdapter(_world, builder.Bounds.Width, builder.Bounds.Height);
        var visiblitySolver = new EntityVisibilitySolver(logicDirectory, densityAdapter);

        Environment = MudEnvironment.Create(sourceAssembly,
            new BaseDmlFramework(_world, logicDirectory, scheduler, visiblitySolver));

        var walkabilityAdapter = new GoRogueWalkabilityAdapter(densityAdapter);
        SenseAdapters.Add(densityAdapter);


        _system = new SequentialSystem<float>(
            new InteractionSystem(_world, visiblitySolver, logicDirectory, Environment.Wrap),
            new UniqueIdentifierSystem(_world),
            new LogicCreationSystem(_world, logicDirectory, entityBuilder, Environment),
            new LogicExecutorSystem(_world, logicDirectory),
            new VerbDiscoverySystem(_world, logicDirectory),
            new MovementSystem(_world, logicDirectory),
            new LogicPropertiesPropagator(_world, logicDirectory),
            new EntityVisualContextCacheSystem(_world, logicDirectory),
            new CommandDiscoverySystem(_world, visiblitySolver),
            new CommandDispatcherService(_world),
            new PathFindingSystem(_world, walkabilityAdapter),
            new ActionSystem<float>(deltaTime => scheduler.Update(deltaTime)),
            new EntityVisionSystem(_world, visiblitySolver)
        );

        builder.Build(_world);

        _world.Subscribe<WorldEchoMessage>(On);
        _world.Subscribe<EntityEchoMessage>(On);
        _world.Subscribe<ConfigureSoundMessage>(On);
    }

    public List<string> WorldMessages { get; } = new();
    public Dictionary<string, List<string>> EntityMessages { get; } = new();


    public List<ConfigureSoundMessage> WorldSoundConfig { get; } = new();
    public Dictionary<string, List<ConfigureSoundMessage>> EntitySoundConfig { get; } = new();

    private void On(in ConfigureSoundMessage message)
    {
        string? name = null;

        if (message.EntityScope.HasValue)
        {
            var logicId = message.EntityScope.Value;
            name = _world.Where(
                e => e.Has<LogicIdentifierComponent>() && e.Get<LogicIdentifierComponent>().LogicInstanceId == logicId
            ).Single().Get<IdentifierComponent>().Name;
        }

        if (name == null)
        {
            WorldSoundConfig.Add(message);
            return;
        }

        if(!EntitySoundConfig.ContainsKey(name))
            EntitySoundConfig[name] = new List<ConfigureSoundMessage>();

        EntitySoundConfig[name].Add(message);

    }

    private void On(in EntityEchoMessage message)
    {
        var logicId = message.Id;

        var name = _world.Where(
            e => e.Has<LogicIdentifierComponent>() && e.Get<LogicIdentifierComponent>().LogicInstanceId == logicId
        ).Single().Get<IdentifierComponent>().Name;

        if (!EntityMessages.ContainsKey(name))
            EntityMessages[name] = new List<string>();

        EntityMessages[name].Add(message.Message);
    }

    private void On(in WorldEchoMessage message)
    {
        WorldMessages.Add(message.Message);
    }

    public void ExecuteVerb(string source, string destination, string verb, string[] arguments)
    {
        var e = _world.CreateEntity();
        e.Set(new ExecuteVerbComponent(source, destination, verb, arguments));
    }

    public string Create(string className)
    {
        var entityBuilder = new BaseEntityBuilder();
        var e = _world.CreateEntity();
        entityBuilder.CreateAtomic(e, className);

        return e.Get<IdentifierComponent>().Name;
    }

    public void Update(float deltaTimeMs)
    {
        _system.Update(deltaTimeMs);

        foreach (var a in SenseAdapters)
            a.ClearCache();
    }

    internal DatumHandle GetHandle(string instance)
    {
        bool isSearch(in IdentifierComponent p)
        {
            return p.Name == instance;
        }

        return logicDirectory[
            _world.GetEntities().With<IdentifierComponent>(isSearch).AsEnumerable().Single()
                .Get<LogicIdentifierComponent>().LogicInstanceId];
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
}