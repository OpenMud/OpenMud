using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(LogicIdentifierComponent))]
[With(typeof(RefreshPropertiesComponent))]
public class LogicPropertiesPropagator : AEntitySetSystem<float>
{
    public static readonly string DEFAULT_ICON_STATE = "_default";

    private readonly LogicDirectory logicLookup;

    public LogicPropertiesPropagator(World world, LogicDirectory logicLookup, bool useBuffer = false) : base(world,
        useBuffer)
    {
        this.logicLookup = logicLookup;
        world.SubscribeComponentAdded<LogicCreatedComponent>(SubscribeRefreshProperties);
    }


    private void SubscribeRefreshProperties(in Entity entity, in LogicCreatedComponent value)
    {
        entity.Set<RefreshPropertiesComponent>();
        var atm = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId].Unwrap<Datum>() as Atom;
        var entityId = entity.Get<IdentifierComponent>().Name;

        if (atm == null)
            return;

        var handler = () =>
        {
            bool matchingIdentifier(in IdentifierComponent identifier)
            {
                return identifier.Name == entityId;
            }

            var e = World.GetEntities().With<IdentifierComponent>(matchingIdentifier).AsEnumerable().FirstOrDefault();

            if (e.IsAlive)
                e.Set<RefreshPropertiesComponent>();
        };

        atm.PropertiesChanged += handler;
        atm.ContentsChanged += handler;
        atm.ContainerChanged += handler;
        atm.IconChanged += handler;
        atm.IconStateChanged += handler;
    }

    private void UpdateIcon(in Entity entity)
    {
        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];

        var newIcon = (string)logic["icon"] ?? "";
        var newIconState = (string)logic["icon_state"] ?? "";

        if (newIconState.Length == 0)
            newIconState = DEFAULT_ICON_STATE;

        if (entity.Has<IconComponent>())
        {
            var ic = entity.Get<IconComponent>();

            if (ic.Icon == newIcon && ic.State == newIcon)
                return;
        }

        entity.Set(new IconComponent(newIcon, newIconState));
    }

    private string[] EnumerateLogicContents(Entity entity)
    {
        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];
        var logicContents = logic["contents"] as DatumHandle;
        var logicHasContents = logicContents != null && logicContents.Type.IsAssignableTo(typeof(DmlList)) &&
                               logicContents["len"] > 0;

        if (!logicHasContents)
            return new string[0];

        var contentsIds = new List<string>();

        for (var i = 1; i <= logicContents["len"]; i++)
        {
            var c = logicContents.ExecProc("Index", i).Result as EntityHandle;

            if (c != null)
            {
                var logicId = logicLookup[c];

                bool matchingLogic(in LogicIdentifierComponent c)
                {
                    return c.LogicInstanceId == logicId;
                }

                var contentsEntity = World.GetEntities().With<IdentifierComponent>()
                    .With<LogicIdentifierComponent>(matchingLogic).AsEnumerable().SingleOrDefault();

                if (contentsEntity.IsAlive)
                    contentsIds.Add(contentsEntity.Get<IdentifierComponent>().Name);
            }
        }

        return contentsIds.ToArray();
    }

    private void UpdateDensity(Entity entity)
    {
        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];
        var isDense = DmlEnv.AsDecimal(logic.Unwrap<Atom>().density) != 0;

        var entityHasDense = entity.Has<DenseComponent>();

        if (isDense != entityHasDense)
        {
            if (isDense)
                entity.Set(new DenseComponent());
            else
                entity.Remove<DenseComponent>();
        }
    }

    private void UpdateDisplayName(Entity entity)
    {
        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];
        var displayName = (string)logic["name"];

        if (displayName == null)
            entity.Remove<DisplayNameComponent>();
        else
            entity.Set(new DisplayNameComponent(displayName));
    }

    private void UpdateContents(Entity entity)
    {
        if (!entity.Has<AtomicContentsComponent>())
            entity.Set(new AtomicContentsComponent(new string[0]));

        var logicContents = EnumerateLogicContents(entity);
        var curContents = entity.Get<AtomicContentsComponent>();

        var logicCount = logicContents.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var curCount = curContents.Contents.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

        var delta = logicCount.Count != curCount.Count || logicCount.Any(x => curCount[x.Key] != x.Value);

        if (delta)
            entity.Set(new AtomicContentsComponent(logicContents));
    }
    /*

    private void UpdateVisible(Entity entity)
    {
        if (!entity.Has<TangibleComponent>())
            return;

        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];

        if (!logic.IsInWorld)
            entity.Remove<VisibleComponent>();
        else if (!entity.Has<VisibleComponent>())
            entity.Set(new VisibleComponent());
    }*/

    protected override void Update(float state, in Entity entity)
    {
        UpdateDensity(entity);
        UpdateContents(entity);
        UpdateDisplayName(entity);
        UpdateIcon(entity);

        entity.Remove<RefreshPropertiesComponent>();
    }
}