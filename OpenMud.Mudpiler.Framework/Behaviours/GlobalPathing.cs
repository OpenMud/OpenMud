using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class GlobalPathing : IRuntimeTypeBuilder
{
    private readonly LogicDirectory logicLookup;
    private readonly World world;

    public GlobalPathing(World world, LogicDirectory logicLookup)
    {
        this.world = world;
        this.logicLookup = logicLookup;
    }

    public bool AcceptsDatum(string target)
    {
        return DmlPath.IsDeclarationInstanceOfPrimitive(target, DmlPrimitive.Global);
    }

    public void Build(DatumHandle handle, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0,
            new ActionDatumProc("walk_to", (args, datum) => WalkTo(args[0], args[1], args[2], args[3], args[4])));
        procedureCollection.Register(0,
            new ActionDatumProc("walk_towards", (args, datum) => WalkTowards(args[0], args[1], args[3], args[4])));
        procedureCollection.Register(0, new ActionDatumProc("get_dir", (args, datum) => VarEnvObjectReference.NULL));
        procedureCollection.Register(0, new ActionDatumProc("flick", (args, datum) => VarEnvObjectReference.NULL));
    }

    private EnvObjectReference WalkTo(EnvObjectReference subject, EnvObjectReference target, EnvObjectReference minDist,
        EnvObjectReference lag, EnvObjectReference speed)
    {
        var entityHandle = logicLookup[subject.Get<Datum>()];
        var ecsEntity = DmlEcs.FindEntity(world, logicLookup, entityHandle);

        var maxDistInt = DmlEnv.AsNumeric(minDist);

        if (typeof(Atom).IsAssignableFrom(target.Type))
        {
            var targetEntity = DmlEcs.FindEntity(world, logicLookup, logicLookup[target.Get<Datum>()]);

            ecsEntity.Set(new PathFindingComponent(targetEntity.Get<IdentifierComponent>().Name, maxDistInt));
        }
        else
        {
            var targetLocation = DmlEnv.ParseCoord(target);

            if (!targetLocation.HasValue)
                ecsEntity.Remove<PathFindingComponent>();
            else
                ecsEntity.Set(new PathFindingComponent(targetLocation.Value.x, targetLocation.Value.y, maxDistInt));
        }

        return VarEnvObjectReference.NULL;
    }

    private EnvObjectReference WalkTowards(EnvObjectReference subject, EnvObjectReference target,
        EnvObjectReference lag, EnvObjectReference speed)
    {
        return WalkTo(subject, target, VarEnvObjectReference.CreateImmutable(0), lag, speed);
    }
}