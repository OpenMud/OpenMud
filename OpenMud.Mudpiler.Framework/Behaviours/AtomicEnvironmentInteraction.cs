using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class AtomicEnvironmentInteraction : IRuntimeTypeBuilder
{
    private static readonly int DEFAULT_VIEW_RANGE = 5;
    private readonly ObjectInstantiator instantiator;
    private readonly LogicDirectory logicLookup;
    private readonly ITypeSolver typeSolver;
    private readonly IEntityVisibilitySolver visibilitySolver;
    private readonly World world;

    public AtomicEnvironmentInteraction(World world, LogicDirectory logicLookup, ObjectInstantiator instantiator,
        ITypeSolver typeSolver, IEntityVisibilitySolver visibilitySolver)
    {
        this.world = world;
        this.logicLookup = logicLookup;
        this.instantiator = instantiator;
        this.typeSolver = typeSolver;
        this.visibilitySolver = visibilitySolver;
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.Atom);
    }

    public void Build(DatumHandle handle, Datum datum, DatumProcCollection procedureCollection)
    {
        var atomicHandle = (EntityHandle)handle;
        var atomic = (Atom)datum;

        procedureCollection.Register(0, new ActionDatumProc("range", args => range(atomicHandle, args)));
        procedureCollection.Register(0, new ActionDatumProc("view", args => view(atomic, args)));
        procedureCollection.Register(0, new ActionDatumProc("oview", args => oview(atomic, args)));
        procedureCollection.Register(0, new ActionDatumProc("Bump", args => VarEnvObjectReference.NULL));
    }

    private IEnumerable<EntityHandle> ComputeVisible(EntityHandle subject, int minDistance, int maxDistance)
    {
        var e = DmlEcs.FindEntity(world, logicLookup, subject);

        return visibilitySolver.ComputeVisible(world, subject, maxDistance)
            .Where(e => e.Value >= minDistance && e.Value <= maxDistance)
            .Select(e => e.Key);

        /*
        if (!e.Has<EntityVisibilityComponent>())
            return Enumerable.Empty<EntityHandle>();

        var visibleNames = e
            .Get<EntityVisibilityComponent>()
            .VisibleEntities
            .Where(e => e.Value >= minDistance && e.Value <= maxDistance)
            .Select(e => e.Key)
            .ToHashSet();

        bool isVisible(in IdentifierComponent c) => visibleNames.Contains(c.Name);

        return world
            .GetEntities()
            .With<IdentifierComponent>(isVisible)
            .With<LogicIdentifierComponent>()
            .AsEnumerable()
            .Select(c => logicLookup[c.Get<LogicIdentifierComponent>().LogicInstanceId]);
        */
    }

    private EnvObjectReference ScopedView(SimpleDmlCoord origin, int min, int max, int offset = 0)
    {
        var minView = min;
        var maxView = 0;

        var listBuffer = instantiator(typeSolver.Lookup("/list"));
        var innerList = listBuffer.Get<DmlList>();

        maxView = max + offset;

        bool withinRange(in PositionComponent p)
        {
            return Math.Sqrt(Math.Pow(p.x - origin.x, 2) + Math.Pow(p.y - origin.y, 2)) <= maxView;
        }

        var subjects = world
            .GetEntities()
            .With<LogicIdentifierComponent>()
            .With<PositionComponent>(withinRange)
            .AsEnumerable()
            .Select(x => logicLookup[x.Get<LogicIdentifierComponent>().LogicInstanceId])
            .ToList();

        foreach (var s in subjects)
            innerList.Add(VarEnvObjectReference.CreateImmutable(s.Unwrap<Datum>()));


        return VarEnvObjectReference.CreateImmutable(listBuffer);
    }

    public EnvObjectReference range(EntityHandle e, ProcArgumentList args)
    {
        var maxView = args[0].Get<int>();
        var centroid = DmlEnv.ParseCoord(args.Get(1, "Center"));

        if (centroid == null)
            centroid = DmlEnv.ParseCoord(e);

        bool withinRange(in PositionComponent p)
        {
            return Math.Sqrt(Math.Pow(p.x - centroid.Value.x, 2) + Math.Pow(p.y - centroid.Value.y, 2)) <= maxView;
        }

        var listBuffer = instantiator(typeSolver.Lookup("/list"));
        var innerList = listBuffer.Get<DmlList>();

        var subjects = world
            .GetEntities()
            .With<LogicIdentifierComponent>()
            .With<PositionComponent>(withinRange)
            .AsEnumerable()
            .Select(x => logicLookup[x.Get<LogicIdentifierComponent>().LogicInstanceId])
            .ToList();

        foreach (var s in subjects)
            innerList.Add(VarEnvObjectReference.CreateImmutable(s.Unwrap<Datum>()));

        return VarEnvObjectReference.CreateImmutable(listBuffer);
    }

    public (SimpleDmlCoord origin, int distance) ParseViewArguments(Atom host, ProcArgumentList args)
    {
        SimpleDmlCoord? centreCoord = null;

        var distArg = args.Get(argName: "Dist").TryGetOrDefault(-1);

        if (!args.Get(argName: "Center").IsNull)
            centreCoord = DmlEnv.ParseCoord(args.Get(argName: "Center"));

        var positional = new[] { args.Get(0), args.Get(1) };

        if (distArg < 0)
            distArg = positional.Select(x => x.TryGetOrDefault(-1)).Where(x => x >= 0).Append(DEFAULT_VIEW_RANGE)
                .First();

        if (centreCoord == null)
            centreCoord = positional.Where(x => !x.IsNull).Select(DmlEnv.ParseCoord).Append(null).First();

        if (centreCoord == null)
            centreCoord = DmlEnv.ParseCoord(host);

        return (centreCoord.Value, distArg);
    }

    public EnvObjectReference view(Atom e, ProcArgumentList args)
    {
        var parsedArgs = ParseViewArguments(e, args);
        return ScopedView(parsedArgs.origin, 0, parsedArgs.distance);
    }

    public EnvObjectReference oview(Atom e, ProcArgumentList args)
    {
        var parsedArgs = ParseViewArguments(e, args);
        return ScopedView(parsedArgs.origin, 1, parsedArgs.distance, 1);
    }
}