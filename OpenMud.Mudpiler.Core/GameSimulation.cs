namespace OpenMud.Mudpiler.Core;

using DefaultEcs;
using DefaultEcs.System;

public interface IGameSimulation
{
    public World World { get; }
    
    public void Update(float deltaTime);
    public void Render(float deltaTime);
}

public class GameSimulation : IGameSimulation
{
    private readonly ISystem<float> renderSystem;
    private readonly ISystem<float> logicSystem;
    public World World { get; }

    public GameSimulation(World ecsWorld, ISystem<float> renderSystem, ISystem<float> logicSystem)
    {
        World = ecsWorld;
        this.renderSystem = renderSystem;
        this.logicSystem = logicSystem;
    }

    public void Update(float deltaTimeSeconds)
    {
        logicSystem.Update(deltaTimeSeconds);
    }

    public void Render(float deltaTimeSeconds)
    {
        renderSystem.Update(deltaTimeSeconds);
    }
}