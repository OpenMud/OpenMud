using System.Reflection;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Core.Systems;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Core;

public struct GameServices
{
    public required LogicDirectory LogicDirectory { get; init; }
    public required IEntityVisibilitySolver VisibilitySolver { get; init; }
    public required GoRogueDensityAdapter DensityAdapter { get; init; }
    public required TimeTaskScheduler TimeTaskScheduler { get; init; }
    public required IMudEntityBuilder EntityBuilder { get; init; }
}

public interface IGameFactory
{
    public IGameSimulation Create();
}

public interface IGameLogicSystemFactory
{
    ISystem<float> Create(World ecsWorld, GameServices services, MudEnvironment environment);
}

public class DefaultGameLogicSystemFactory : IGameLogicSystemFactory
{
    public ISystem<float> Create(World ecsWorld, GameServices services, MudEnvironment environment)
    {
        var walkabilityAdapter = new GoRogueWalkabilityAdapter(services.DensityAdapter);

        return new SequentialSystem<float>(
            //Important that the first thing we do is clear the senses cache so that subsequent systems have a fresh slate.
            new ActionSystem<float>(_ => services.DensityAdapter.ClearCache()),
            new AtomicCreationSystem(ecsWorld, environment, services.EntityBuilder),
            new InteractionSystem(ecsWorld, services.VisibilitySolver, services.LogicDirectory, environment.Wrap),
            new UniqueIdentifierSystem(ecsWorld),
            new LogicCreationSystem(ecsWorld, services.LogicDirectory, services.EntityBuilder, environment),
            new LogicExecutorSystem(ecsWorld, services.LogicDirectory),
            new VerbDiscoverySystem(ecsWorld, services.LogicDirectory),
            new MovementSystem(ecsWorld, services.LogicDirectory),
            new LogicPropertiesPropagator(ecsWorld, services.LogicDirectory),
            new EntityVisualContextCacheSystem(ecsWorld, services.LogicDirectory),
            new CommandDiscoverySystem(ecsWorld, services.VisibilitySolver),
            new CommandDispatcherService(ecsWorld),
            new CommandParserSystem(ecsWorld),
            new PathFindingSystem(ecsWorld, walkabilityAdapter),
            new ActionSystem<float>(deltaTime => services.TimeTaskScheduler.Update(deltaTime)),
            new GameFlowSystem(ecsWorld, services.LogicDirectory),
            new EntityVisionSystem(ecsWorld, services.VisibilitySolver)
        );
    }
}

public abstract class BaseGameFactory(IGameLogicSystemFactory logicSystemFactory) : IGameFactory
{
    protected readonly IGameLogicSystemFactory LogicSystemFactory = logicSystemFactory;

    protected abstract IGameSimulation CreateGame(World world, GameServices services, ISystem<float> logicSystem);
    protected abstract MudEnvironment CreateMudEnvironment(World ecsWorld, GameServices services);
    protected abstract IMudSceneBuilder CreateSceneBuilder();

    protected virtual IMudEntityBuilder CreateEntityBuilder()
    {
        return new BaseEntityBuilder();
    }
    
    public IGameSimulation Create()
    {
        var world = new World();
        var sceneBuilder = CreateSceneBuilder();
        var services = CreateServices(world, sceneBuilder.Bounds.Width, sceneBuilder.Bounds.Height);
        var environment = CreateMudEnvironment(world, services);
        var logicSystem = LogicSystemFactory.Create(
                world,
                services,
                environment
        );/*CreateLogicSystem(
                world,
                services,
                environment
            );*/
        
        sceneBuilder.Build(world);
        
        return CreateGame(world, services, logicSystem);
    }

    protected GameServices CreateServices(World ecsWorld, int boundsWidth, int boundsHeight)
    {
        var logicDirectory = new LogicDirectory();
        var scheduler = new TimeTaskScheduler();
        
        var densityAdapter = new GoRogueDensityAdapter(ecsWorld, boundsWidth, boundsHeight);
        var visibilitySolver = new EntityVisibilitySolver(logicDirectory, densityAdapter);

        return new GameServices()
        {
            DensityAdapter = densityAdapter,
            LogicDirectory = logicDirectory,
            VisibilitySolver = visibilitySolver,
            TimeTaskScheduler = scheduler,
            EntityBuilder = CreateEntityBuilder()
        };
    }
}