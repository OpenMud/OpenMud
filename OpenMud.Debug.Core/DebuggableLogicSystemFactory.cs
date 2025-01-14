using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Debug.Core;


public sealed class DebuggableLogicSystemFactory(IGameLogicSystemFactory logicSystemFactory) : IGameLogicSystemFactory
{
    private IGameLogicSystemFactory logicSystemFactory = logicSystemFactory;

    public ISystem<float> Create(World ecsWorld, GameServices services, MudEnvironment environment)
    {
        var baseLogic = logicSystemFactory.Create(ecsWorld, services, environment);

        return new SequentialSystem<float>(new[]
        {
            baseLogic,
            new ActionSystem<float>(_ => services.DensityAdapter.ClearCache())
        });
    }
}