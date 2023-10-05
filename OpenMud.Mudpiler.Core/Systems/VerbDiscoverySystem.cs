using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(LogicIdentifierComponent), typeof(VerbDetailsComponent), typeof(RefreshVerbsComponent))]
public class VerbDiscoverySystem : AEntitySetSystem<float>
{
    private readonly LogicDirectory logicDirectory;

    public VerbDiscoverySystem(World world, LogicDirectory logicDirectory, bool useBuffer = false) :
        base(world, useBuffer)
    {
        this.logicDirectory = logicDirectory;

        world.SubscribeComponentAdded<LogicCreatedComponent>(SubscribeRefreshVerbDetails);
    }

    private void SubscribeRefreshVerbDetails(in Entity entity, in LogicCreatedComponent value)
    {
        entity.Set<RefreshVerbsComponent>();
        var atm = logicDirectory[entity.Get<LogicIdentifierComponent>().LogicInstanceId].Unwrap<Datum>() as Atom;
        var entityId = entity.Get<IdentifierComponent>().Name;

        if (atm == null)
            return;

        bool matchingIdentifier(in IdentifierComponent identifier)
        {
            return identifier.Name == entityId;
        }

        atm.VerbsChanged += () =>
        {
            var e = World.GetEntities().With<IdentifierComponent>(matchingIdentifier).AsEnumerable().FirstOrDefault();

            if (e.IsAlive)
                e.Set<RefreshVerbsComponent>();
        };
    }

    protected override void Update(float state, in Entity entity)
    {
        var attr = entity.Get<VerbDetailsComponent>();
        var eh = logicDirectory[entity.Get<LogicIdentifierComponent>().LogicInstanceId];

        attr.Verbs.Clear();

        foreach (var (verb, details) in Discover(eh))
            attr.Verbs[verb.ToLower()] = details;

        entity.Remove<RefreshVerbsComponent>();
        base.Update(state, entity);
    }

    private Dictionary<string, VerbDetails> Discover(EntityHandle eh)
    {
        var datum = eh.Unwrap<Datum>() as Atom;

        if (datum == null || datum.Verbs.Length == 0)
            return new Dictionary<string, VerbDetails>();

        return datum.Verbs.ToDictionary(
            v => v.verbName,
            v => new VerbDetails(
                v.procName,
                v.category,
                v.source,
                v.argAsConstraints,
                v.argSourceConstraints,
                v.listEvalSourceConstraint
            )
        );
    }
}