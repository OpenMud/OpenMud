using DefaultEcs;

namespace OpenMud.Mudpiler.Core.Scene;

public struct WorldBounds
{
    public readonly int Width;
    public readonly int Height;

    public WorldBounds(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

public interface IMudSceneBuilder
{
    WorldBounds Bounds => new(0, 0);
    void Build(World world);
}

public class NullSceneBuilder : IMudSceneBuilder
{
    public NullSceneBuilder(int width, int height)
    {
        Bounds = new WorldBounds(width, height);
    }

    public WorldBounds Bounds { get; }

    public void Build(World world)
    {
    }
}