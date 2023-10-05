using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Core.Systems;

[WithEither(typeof(SlideComponent), typeof(MovementCoolDownComponent))]
[With(typeof(PositionComponent))]
public class MovementSystem : AEntitySetSystem<float>
{
    private readonly LogicDirectory logicLookup;

    public MovementSystem(World world, LogicDirectory logicLookup, bool useBuffer = false) : base(world, useBuffer)
    {
        this.logicLookup = logicLookup;
    }

    private bool DetectAndBroadcastCollision(Entity subject)
    {
        var slide = subject.Get<SlideComponent>();

        if (!slide.SkipLogicChecks && !SlideConstraintSolver.TestAllowSlide(logicLookup, World, subject, slide.DeltaX,
                slide.DeltaY, out var collider))
        {
            World.Publish(new CollisionMessage(subject, collider));
            return true;
        }

        var currentPosition = subject.Get<PositionComponent>();
        var (newX, newY) = (currentPosition.x + slide.DeltaX, currentPosition.y + slide.DeltaY);
        subject.Set(new DirectionComponent(DmlEnv.AsDirection(slide.DeltaX, slide.DeltaY)));
        subject.Set(new PositionComponent(newX, newY, currentPosition.z));

        SlideConstraintSolver.DispatchMovedLogic(logicLookup, World, subject,
            new SimpleDmlCoord(currentPosition.x, currentPosition.y, currentPosition.z));

        return false;
    }

    protected override void Update(float state, in Entity entity)
    {
        if (entity.Has<MovementCoolDownComponent>())
        {
            var cooldown = entity.Get<MovementCoolDownComponent>();
            cooldown.LifeTime -= state;

            if (cooldown.LifeTime <= 0)
                entity.Remove<MovementCoolDownComponent>();
        }

        if (entity.Has<SlideComponent>() && !entity.Has<MovementCoolDownComponent>())
        {
            if (!DetectAndBroadcastCollision(entity))
            {
                var movement = entity.Get<SlideComponent>();
                entity.Set(new MovementCoolDownComponent(movement.TimeCost));
            }

            if (!entity.Get<SlideComponent>().Persist)
                entity.Remove<SlideComponent>();
        }
    }
}