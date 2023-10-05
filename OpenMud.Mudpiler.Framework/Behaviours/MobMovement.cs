using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class MobMovement : IRuntimeTypeBuilder
{
    private static readonly int DEFAULT_VIEW_RANGE = 5;
    private readonly LogicDirectory logicLookup;
    private readonly World world;

    public MobMovement(World world, LogicDirectory logicLookup)
    {
        this.world = world;
        this.logicLookup = logicLookup;
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.Mob);
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0,
            new ActionDatumProc("Move", (args, datum) => Move((Atom)datum, args[0], args[1], args[3], args[3])));
    }

    private bool Slide(DatumHandle datumHandle, int deltaX, int deltaY)
    {
        var entityHandle = datumHandle as EntityHandle;

        if (entityHandle == null)
            throw new Exception("Datum cannot slide...");

        var ecsEntity = DmlEcs.FindEntity(world, logicLookup, entityHandle);

        if (!SlideConstraintSolver.TestAllowSlide(logicLookup, world, ecsEntity, deltaX, deltaY, out var collision))
        {
            world.Publish(new CollisionMessage(ecsEntity, collision));
            return false;
        }

        ecsEntity.Set(new SlideComponent(deltaX, deltaY, MovementCost.Compute(deltaX, deltaY), true));

        return true;
    }

    private EnvObjectReference Move(Atom datum, EnvObjectReference dest, EnvObjectReference dir,
        EnvObjectReference stepX, EnvObjectReference stepY)
    {
        DmlCoord? destCoord = null;
        if (typeof(Atom).IsAssignableFrom(dest.Type))
            destCoord = ((Atom)dest.Target).loc.Get<DmlCoord>();
        else if (typeof(DmlCoord).IsAssignableFrom(dest.Type))
            destCoord = dest.Get<DmlCoord>();

        if (destCoord == null)
            throw new NotImplementedException();

        var currentLoc = datum.loc.Get<DmlCoord>();
        int deltaX = Math.Abs(currentLoc.x.Get<int>() - destCoord.x.Get<int>());
        int deltaY = Math.Abs(currentLoc.y.Get<int>() - destCoord.y.Get<int>());

        var isSlide =
            deltaX <= 1 &&
            deltaY <= 1;

        if (isSlide)
        {
            var e = logicLookup[datum];
            var allowSlide = Slide(e, deltaX, deltaY);

            return VarEnvObjectReference.CreateImmutable(allowSlide ? 1 : 0);
        }

        return Jump(datum, dest, dir, stepX, stepY);
    }

    public EnvObjectReference Jump(Atom datum, EnvObjectReference dest, EnvObjectReference dir,
        EnvObjectReference stepX, EnvObjectReference stepY)
    {
        DmlCoord? destCoord = null;
        if (typeof(Atom).IsAssignableFrom(dest.Type))
            destCoord = ((Atom)dest.Target).loc.Get<DmlCoord>();
        else if (typeof(DmlCoord).IsAssignableFrom(dest.Type))
            destCoord = dest.Get<DmlCoord>();

        if (destCoord == null)
            throw new NotImplementedException();

        datum.x.Assign(destCoord.x);
        datum.y.Assign(destCoord.y);
        //datum.layer.Assign(destCoord.z);
        return VarEnvObjectReference.CreateImmutable(1);
    }
}