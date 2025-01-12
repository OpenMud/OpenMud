using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Client.Terminal.Systems;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using SadConsole.UI.Controls;

namespace OpenMud.Mudpiler.Client.Terminal;

public sealed class TerminalGameFactory : BaseGameFactory
{
    private DmeProject Project { get; }

    private DrawingArea? DrawingArea { get; } = null;
    
    public TerminalGameFactory(DmeProject project)
    {
        this.Project = project;
    }

    private TerminalGameFactory(DmeProject project, DrawingArea drawingArea)
    {
        this.Project = project;
        this.DrawingArea = drawingArea;
    }

    public TerminalGameFactory SetRenderTarget(DrawingArea drawingArea)
    {
        return new TerminalGameFactory(Project, drawingArea);
    }

    protected override IGameSimulation CreateGame(World ecsWorld, GameServices services, ISystem<float> logicSystem)
    {
        ISystem<float> renderSystem = new ActionSystem<float>(_ => { });
        
        if (DrawingArea != null)
            renderSystem = CreateRenderSystem(ecsWorld, services, DrawingArea);

        var addSystems = new List<ISystem<float>>
        {
            new TerminalAnimatorSystem(ecsWorld),
            new TerminalAnimationBuilderSystem(ecsWorld, services.LogicDirectory)
        };

        logicSystem = new SequentialSystem<float>(addSystems.Prepend(logicSystem));

        return new GameSimulation(ecsWorld, renderSystem, logicSystem);
    }

    protected override MudEnvironment CreateMudEnvironment(World ecsWorld, GameServices services)
    {
        return MudEnvironment.Create(Project.Logic,
            new BaseDmlFramework(ecsWorld, services.LogicDirectory, services.TimeTaskScheduler, services.VisibilitySolver));
    }

    protected override IMudSceneBuilder CreateSceneBuilder()
    {
        var sceneBuilder = Project.Maps.Values.Single();

        return sceneBuilder;
    }
    
    private ISystem<float> CreateRenderSystem(World ecsWorld, GameServices services, DrawingArea renderTarget)
    {
        return new WorldTerminalRenderSystem(services.VisibilitySolver, ecsWorld, renderTarget);
    }
}
