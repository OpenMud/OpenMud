using DefaultEcs;

namespace OpenMud.Mudpiler.Core.Scene;

public class SimpleSceneBuilder : IMudSceneBuilder
{
    private readonly Action<World> builder;

    public SimpleSceneBuilder(WorldBounds bounds, Action<World> builder)
    {
        this.builder = builder;
        Bounds = bounds;
    }

    public WorldBounds Bounds { get; }

    public void Build(World world)
    {
        builder(world);
    }
}