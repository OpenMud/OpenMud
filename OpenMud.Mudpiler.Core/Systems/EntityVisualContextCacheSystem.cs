using System.Collections.Immutable;
using DefaultEcs;
using DefaultEcs.System;
using GoRogue;
using GoRogue.SenseMapping;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Core.Systems;

public class EntityVisibilitySolver : IEntityVisibilitySolver
{
    public static readonly int DEFAULT_VIEW_DISTANCE = 20;
    public static readonly float VIEW_AGE_EXPIRE = .01f;

    private readonly GoRogueDensityAdapter densityAdapter;
    private readonly LogicDirectory logicDirectory;

    public EntityVisibilitySolver(LogicDirectory logicDirectory, GoRogueDensityAdapter densityAdapter)
    {
        this.densityAdapter = densityAdapter;
        this.logicDirectory = logicDirectory;
    }

    public IImmutableDictionary<EntityHandle, float> ComputeVisible(World world, EntityHandle entity,
        int fieldOfView = -1)
    {
        var logicId = logicDirectory[entity];

        bool isLogicIdentifier(in LogicIdentifierComponent id)
        {
            return id.LogicInstanceId == logicId;
        }

        var ecsEntity = world.GetEntities().With<LogicIdentifierComponent>(isLogicIdentifier).AsEnumerable().First();

        var visible = ComputeVisible(world, ecsEntity, fieldOfView);

        bool isEntityVisible(in IdentifierComponent e)
        {
            return visible.ContainsKey(e.Name);
        }

        return world
            .GetEntities()
            .With<LogicIdentifierComponent>()
            .With<IdentifierComponent>(isEntityVisible)
            .AsEnumerable()
            .ToImmutableDictionary(
                e => logicDirectory[e.Get<LogicIdentifierComponent>().LogicInstanceId],
                e => visible[e.Get<IdentifierComponent>().Name]
            );
    }

    private static IImmutableDictionary<string, float> TruncateRange(IImmutableDictionary<string, float> known,
        int range)
    {
        return known.Where(x => x.Value <= range).ToImmutableDictionary(x => x.Key, x => x.Value);
    }

    public IImmutableDictionary<string, float> ComputeVisible(World world, in Entity entity, int fieldOfView = -1)
    {
        if (fieldOfView < 0)
            fieldOfView = DEFAULT_VIEW_DISTANCE;

        if (HasValidVisible(entity, fieldOfView))
            return TruncateRange(entity.Get<EntityVisualContextCacheComponent>().VisibleEntities, fieldOfView);

        var curLoc = entity.Get<PositionComponent>();

        var src = new SenseSource(SourceType.RIPPLE, new Coord(curLoc.x, curLoc.y), fieldOfView, Distance.EUCLIDEAN);

        var visibilityMap = new SenseMap(densityAdapter);
        visibilityMap.AddSenseSource(src);
        visibilityMap.Calculate();
        visibilityMap.RemoveSenseSource(src);

        var newlySensed = visibilityMap.NewlyInSenseMap.Select(x => Tuple.Create(x.X, x.Y)).ToHashSet();

        bool isSensed(in PositionComponent p)
        {
            return newlySensed.Contains(Tuple.Create(p.x, p.y));
        }

        double computeDistance(in PositionComponent p)
        {
            return Coord.EuclideanDistanceMagnitude(curLoc.x - p.x, curLoc.y - p.y);
        }

        var visible = world
            .GetEntities()
            .With<VisibleComponent>()
            .With<IdentifierComponent>()
            .With<PositionComponent>(isSensed)
            .AsEnumerable()
            .ToImmutableDictionary(
                x => x.Get<IdentifierComponent>().Name,
                x => (float)computeDistance(x.Get<PositionComponent>())
            );

        var newVisible = new EntityVisualContextCacheComponent(visible, fieldOfView);

        UpdateVisibleComponent(entity, newVisible);

        return TruncateRange(visible, fieldOfView);
    }

    private bool HasValidVisible(Entity entity, int fieldOfView)
    {
        if (!entity.Has<EntityVisualContextCacheComponent>())
            return false;

        if (entity.Get<EntityVisualContextCacheComponent>().Range >= fieldOfView)
            return false;

        if (entity.Has<EntityVisualContextCacheAgeComponent>() &&
            entity.Get<EntityVisualContextCacheAgeComponent>().Age >= VIEW_AGE_EXPIRE)
            return false;

        return true;
    }

    private void UpdateVisibleComponent(in Entity entity, EntityVisualContextCacheComponent newVisible)
    {
        entity.Set(new EntityVisualContextCacheAgeComponent(0));
        entity.Set(newVisible);
    }
}

[WithEither(typeof(RefreshVisibilityComponent), typeof(EntityVisualContextCacheComponent))]
public class EntityVisualContextCacheSystem : AEntitySetSystem<float>
{
    private readonly LogicDirectory logicLookup;

    public EntityVisualContextCacheSystem(World world, LogicDirectory logicLookup, bool useBuffer = false) : base(world,
        useBuffer)
    {
        this.logicLookup = logicLookup;
        world.SubscribeComponentAdded<LogicCreatedComponent>(SubscribeRefreshProperties);
    }

    private void SubscribeRefreshProperties(in Entity entity, in LogicCreatedComponent value)
    {
        entity.Set<RefreshVisibilityComponent>();
        var atm = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId].Unwrap<Datum>() as Atom;
        var entityId = entity.Get<IdentifierComponent>().Name;

        if (atm == null)
            return;

        bool matchingIdentifier(in IdentifierComponent identifier)
        {
            return identifier.Name == entityId;
        }

        var handler = () =>
        {
            var e = World.GetEntities().With<IdentifierComponent>(matchingIdentifier).AsEnumerable().FirstOrDefault();

            if (e.IsAlive)
                e.Set<RefreshVisibilityComponent>();
        };

        atm.ContainerChanged += handler;
    }

    private void UpdateVisible(in Entity entity)
    {
        if (!entity.Has<TangibleComponent>())
            return;

        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];

        if (!logic.IsInWorld)
            entity.Remove<VisibleComponent>();
        else if (!entity.Has<VisibleComponent>())
            entity.Set(new VisibleComponent());
    }

    protected override void Update(float state, in Entity entity)
    {
        var currentAge = entity.Has<EntityVisualContextCacheAgeComponent>()
            ? entity.Get<EntityVisualContextCacheAgeComponent>().Age
            : 0;
        entity.Set(new EntityVisualContextCacheAgeComponent(currentAge + state));

        if (entity.Has<RefreshVisibilityComponent>())
        {
            UpdateVisible(in entity);
            entity.Remove<RefreshVisibilityComponent>();
        }
    }
}