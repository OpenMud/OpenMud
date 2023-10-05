namespace OpenMud.Mudpiler.Core.Components;

public class MovementCoolDownComponent
{
    public float LifeTime;

    public MovementCoolDownComponent(float lifeTime)
    {
        LifeTime = lifeTime;
    }
}