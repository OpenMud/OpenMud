using DefaultEcs;
using DefaultEcs.System;
using GoRogue;
using GoRogue.Pathing;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Core.Systems;

[Without(typeof(SlideComponent), typeof(MovementCoolDownComponent))]
[With(typeof(PathFindingComponent), typeof(PositionComponent))]
public class PathFindingSystem : AEntitySetSystem<float>
{
    private readonly GoRogueWalkabilityAdapter walkabilityMap;

    public PathFindingSystem(World world, GoRogueWalkabilityAdapter walkabilityMap, bool useBuffer = false) : base(
        world, useBuffer)
    {
        this.walkabilityMap = walkabilityMap;
    }

    protected override void Update(float state, in Entity entity)
    {
        //The "distance" function also defines the cardinality of movement (4 vs 8 direction)...
        var pather = new AStar(walkabilityMap, Distance.MANHATTAN);

        var destComponent = entity.Get<PathFindingComponent>();
        var current = entity.Get<PositionComponent>();

        var destCoord = new SimpleDmlCoord(destComponent.DestinationX, destComponent.DestinationY, current.z);

        if (destComponent.TargetEntity != null)
        {
            bool isTargetEntity(in IdentifierComponent p)
            {
                return p.Name == destComponent.TargetEntity;
            }

            var targetEntity = World.GetEntities()
                .With<PositionComponent>()
                .With<IdentifierComponent>(isTargetEntity)
                .AsEnumerable().SingleOrDefault();

            if (!targetEntity.IsAlive)
            {
                World.Publish(new PathingFailedMessage(entity));
                entity.Remove<PathFindingComponent>();
                return;
            }

            var targetPosition = targetEntity.Get<PositionComponent>();

            destCoord = new SimpleDmlCoord(targetPosition.x, targetPosition.y, current.z);
        }

        var dist = Math.Sqrt(Math.Pow(destCoord.x - current.x, 2) + Math.Pow(destCoord.y - current.y, 2));

        if (dist <= destComponent.HaltDistance)
            return;

        var path = pather.ShortestPath(new Coord(current.x, current.y), new Coord(destCoord.x, destCoord.y));

        if (path == null || path.Length == 0)
        {
            World.Publish(new PathingFailedMessage(entity));
            entity.Remove<PathFindingComponent>();
            return;
        }

        var next = path.GetStep(0);

        var deltaX = next.X - current.x;
        var deltaY = next.Y - current.y;

        entity.Set(new SlideComponent(deltaX, deltaY, MovementCost.Compute(deltaX, deltaY)));
    }
}