using System.Collections.Immutable;

namespace OpenMud.Mudpiler.Core.Components;

public struct EntityVisualContextCacheAgeComponent
{
    public readonly float Age = 0;

    public EntityVisualContextCacheAgeComponent(float age)
    {
        Age = age;
    }
}

public struct EntityVisualContextCacheComponent
{
    public readonly IImmutableDictionary<string, float> VisibleEntities;
    public readonly int Range;

    public EntityVisualContextCacheComponent(IImmutableDictionary<string, float> visibleEntities, int range)
    {
        VisibleEntities = visibleEntities;
        Range = range;
    }
}