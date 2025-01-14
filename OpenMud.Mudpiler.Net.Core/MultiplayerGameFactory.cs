using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.RuntimeTypes;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Net.Core;

public class MultiplayerGameFactory(
    string projectDirectory,
    IDmlFrameworkFactory frameworkFactory,
    IGameLogicSystemFactory logicSystemFactory)
    : BaseGameFactory(logicSystemFactory)
{
    private DmeProject Project { get; } = DmeProject.Load(projectDirectory, new BaseEntityBuilder());
    private IDmlFrameworkFactory FrameworkFactory { get; } = frameworkFactory;

    protected override IGameSimulation CreateGame(World ecsWorld, GameServices services, ISystem<float> logicSystem)
    {
        ISystem<float> renderSystem = new ActionSystem<float>(_ => { });
        
        return new GameSimulation(ecsWorld, renderSystem, logicSystem);
    }

    protected override MudEnvironment CreateMudEnvironment(World ecsWorld, GameServices services)
    {
        return MudEnvironment.Create(
            Project.Logic,
            FrameworkFactory.Create(ecsWorld, services.LogicDirectory, services.TimeTaskScheduler, services.VisibilitySolver)
        );
    }

    protected override IMudSceneBuilder CreateSceneBuilder()
    {
        var sceneBuilder = Project.Maps.Values.Single();

        return sceneBuilder;
    }
}