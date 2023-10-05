using DefaultEcs;

namespace OpenMud.Mudpiler.Core.Scene;

public interface IMudEntityBuilder
{
    void CreateAtomic(Entity entity, string className, string? identifier = null, int initialX = 0, int initialY = 0,
        int? initialZ = null);
}