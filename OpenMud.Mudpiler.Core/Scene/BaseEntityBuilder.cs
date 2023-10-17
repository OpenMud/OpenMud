using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Core.Scene;

public class BaseEntityBuilder : IMudEntityBuilder
{
    public void CreateAtomic(Entity entity, string className, string? identifier = null, int initialX = 0,
        int initialY = 0, int? initialZ = null)
    {
        entity.Set(new RequestIdentifierComponent(identifier));
        entity.Set(new CreateLogicComponent { ClassName = className });

        if (initialZ == null)
            initialZ = AtomicDefaults.IdentifyDefaultLayer(className);

        if (AtomicDefaults.CanSee(className))
            entity.Set(new VisionComponent(AtomicDefaults.SightRange(className)));

        if (AtomicDefaults.IsTangible(className))
            entity.Set(new TangibleComponent());

        entity.Set(new PositionComponent(initialX, initialY, (int)initialZ));
    }
}