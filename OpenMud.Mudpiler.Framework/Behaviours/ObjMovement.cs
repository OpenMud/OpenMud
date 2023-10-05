using DefaultEcs;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class ObjMovement : IRuntimeTypeBuilder
{
    private readonly MobMovement mobMovement;

    public ObjMovement(World world, LogicDirectory logicLookup)
    {
        mobMovement = new MobMovement(world, logicLookup);
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.Obj);
    }


    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        var atomicHandle = (EntityHandle)e;

        procedureCollection.Register(0,
            new ActionDatumProc("Move", (args, datum) => Move((Atom)datum, args[0], args[1], args[3], args[3])));
    }

    public EnvObjectReference Move(Atom datum, EnvObjectReference dest, EnvObjectReference dir,
        EnvObjectReference stepX, EnvObjectReference stepY)
    {
        if (dest.TryGet<Atom>(out var a) &&
            RuntimeTypeResolver.InheritsBaseTypeDatum(a.type, DmlPrimitiveBaseType.Movable))
        {
            ((Atom)dest.Target).contents.Get<DmlList>().Add(VarEnvObjectReference.CreateImmutable(datum));
            return VarEnvObjectReference.CreateImmutable(1);
        }

        var world = (EnvObjectReference)((EnvObjectReference)datum.ctx.global).Get<Global>().world;

        var moveResult = mobMovement.Jump(datum, dest, dir, stepX, stepY);
        //Move it into the world.
        if (DmlEnv.AsLogical(moveResult))
            ((EnvObjectReference)world.Get<GameWorld>().contents).Get<DmlList>()
                .Add(VarEnvObjectReference.CreateImmutable(datum));

        return moveResult;
    }
}