﻿using DefaultEcs;
using GoRogue;
using GoRogue.MapViews;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Core;

public interface GoRogueSenseAdapter : IMapView<double>
{
    void ClearCache();
}

public sealed class GoRogueWalkabilityAdapter : IMapView<bool>
{
    private readonly GoRogueSenseAdapter source;

    public GoRogueWalkabilityAdapter(GoRogueSenseAdapter source)
    {
        this.source = source;
    }

    public bool this[Coord pos] => source[pos] < 0.5;

    public bool this[int index1D] => source[index1D] < 0.5;

    public bool this[int x, int y] => source[x, y] < 0.5;

    public int Height => source.Height;

    public int Width => source.Width;
}

public sealed class GoRogueDensityAdapter : GoRogueSenseAdapter
{
    private Dictionary<Coord, double>? coordCache = null;
    private readonly World world;

    public GoRogueDensityAdapter(World world, int worldWidth, int worldHeight)
    {
        this.world = world;
        this.Width = worldWidth;
        this.Height = worldHeight;
    }

    public void ClearCache()
    {
        coordCache = null;
    }

    public Dictionary<Coord, double> BuildCoordCache(Coord origin)
    {
        if (coordCache != null)
            return coordCache;

        var posMap = new Dictionary<Coord, List<Entity>>();

        foreach (var r in world.GetEntities().With<PositionComponent>().AsEnumerable())
        {
            var p = r.Get<PositionComponent>();
            var c = new Coord(p.x, p.y);

            if(!posMap.ContainsKey(c))
                posMap[c] = new List<Entity>();

            posMap[c].Add(r);
        }

        coordCache = new();

        foreach (var (k, v) in posMap)
            coordCache[k] = Compute(v);

        return coordCache;
    }

    public double this[Coord pos]
    {
        get
        {
            if (BuildCoordCache(pos).TryGetValue(pos, out var val))
                return val;

            return 0;
        }
    }

    public double this[int index1D] => this[Coord.ToCoord(index1D, Width)];

    public double this[int x, int y] => this[new Coord(x, y)];

    public int Height { get; }

    public int Width { get; }

    private static double Compute(IEnumerable<Entity> e)
    {
        if (e.Any(n => n.Has<DenseComponent>()))
            return 1.0;

        return 0.0;
    }
}