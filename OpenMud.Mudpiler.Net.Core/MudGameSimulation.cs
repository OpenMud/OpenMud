using System.Reflection;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.RuntimeTypes;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Core.Systems;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.Net.Core.Encoding;
using OpenMud.Mudpiler.Net.Core.Systems;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Net.Core;

public class MudGameSimulation : IGameSimulation
{
    private readonly ISystem<float> _gameSystem;
    private readonly ISystem<ServerFrame> _serverSystem;

    private readonly List<GoRogueSenseAdapter> senseAdapters = new();

    public MudGameSimulation(IMudSceneBuilder builder, Assembly sourceAssembly, IWorldStateEncoderFactory worldEncoder,
        IClientDispatcher clientDispatcher, IClientReceiver clientReceiver, IDmlFrameworkFactory frameworkFactory)
    {
        var logicDirectory = new LogicDirectory();
        var entityBuilder = new BaseEntityBuilder();
        var scheduler = new TimeTaskScheduler();
        var EcsWorld = new World();
        var densityAdapter = new GoRogueDensityAdapter(EcsWorld, builder.Bounds.Width, builder.Bounds.Height);

        var walkabilityAdapter = new GoRogueWalkabilityAdapter(densityAdapter);
        var visibilitySolver = new EntityVisibilitySolver(logicDirectory, densityAdapter);

        var environment = MudEnvironment.Create(sourceAssembly,
            frameworkFactory.Create(EcsWorld, logicDirectory, scheduler, visibilitySolver));

        senseAdapters.Add(densityAdapter);

        _gameSystem = new SequentialSystem<float>(
            new InteractionSystem(EcsWorld, visibilitySolver, logicDirectory, environment.Wrap),
            new UniqueIdentifierSystem(EcsWorld),
            new LogicCreationSystem(EcsWorld, logicDirectory, entityBuilder, environment),
            new LogicExecutorSystem(EcsWorld, logicDirectory),
            new VerbDiscoverySystem(EcsWorld, logicDirectory),
            new MovementSystem(EcsWorld, logicDirectory),
            new LogicPropertiesPropagator(EcsWorld, logicDirectory),
            new EntityVisualContextCacheSystem(EcsWorld, logicDirectory),
            new CommandDiscoverySystem(EcsWorld, visibilitySolver),
            new CommandDispatcherService(EcsWorld),
            new CommandParserSystem(EcsWorld),
            new PathFindingSystem(EcsWorld, walkabilityAdapter),
            new ActionSystem<float>(deltaTime => scheduler.Update(deltaTime)),
            new GameFlowSystem(EcsWorld, logicDirectory),
            new EntityVisionSystem(EcsWorld, visibilitySolver)
        );

        _serverSystem = new SequentialSystem<ServerFrame>(
            new NetworkWorldRequestRealizerSystem(EcsWorld, clientReceiver),
            new NetworkWorldSynchronizerSystem(EcsWorld, clientDispatcher, worldEncoder),
            new GamePlayerResourceSystem(EcsWorld, entityBuilder, environment.World.Unwrap<GameWorld>())
        );

        builder.Build(EcsWorld);
    }

    public void Update(ServerFrame delta)
    {
        _gameSystem.Update(delta.DeltaSeconds);
        _serverSystem.Update(delta);

        foreach (var a in senseAdapters)
            a.ClearCache();
    }
}