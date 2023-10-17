using System.Collections.Immutable;

namespace OpenMud.Mudpiler.Core.Components;

public struct EntityVisibilityComponent
{
    public readonly IImmutableSet<string> VisibleEntities;

    public EntityVisibilityComponent(IImmutableSet<string> visibleEntities)
    {
        VisibleEntities = visibleEntities;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityVisibilityComponent component && VisibleEntities.Count == component.VisibleEntities.Count &&
               VisibleEntities.Intersect(component.VisibleEntities).Count == VisibleEntities.Count;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VisibleEntities);
    }
}