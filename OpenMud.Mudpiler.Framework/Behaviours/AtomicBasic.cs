using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class AtomicBasic : IRuntimeTypeBuilder
{
    private static readonly int DEFAULT_VIEW_RANGE = 5;
    private readonly ObjectInstantiator instantiator;
    private readonly LogicDirectory logicLookup;
    private readonly World world;

    public AtomicBasic(World world, LogicDirectory logicLookup, ObjectInstantiator instantiator)
    {
        this.world = world;
        this.logicLookup = logicLookup;
        this.instantiator = instantiator;
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.Atom);
    }

    public void Build(DatumHandle handle, Datum datum, DatumProcCollection procedureCollection)
    {
        var atomic = (Atom)datum;

        var (xBind, yBind, zBind, locBind) = BindLocation(handle);
        var dirBind = BindDirection(handle);

        atomic.x.Bind(xBind);
        atomic.y.Bind(yBind);
        atomic.layer.Bind(zBind);
        atomic.loc.Bind(locBind);

        atomic.dir.Bind(dirBind);

        procedureCollection.Register(0, new ActionDatumProc("New", (args, datum) => New((Atom)datum, args)));
    }

    private (ManagedEnvObjectReference x, ManagedEnvObjectReference y, ManagedEnvObjectReference z,
        ManagedEnvObjectReference loc) BindLocation(DatumHandle e)
    {
        var entityHandle = e as EntityHandle;

        if (entityHandle == null)
            throw new Exception("Data binding not supported for datum handles");

        var isArea = RuntimeTypeResolver.InheritsBaseTypeDatum(entityHandle["type"], DmlPrimitiveBaseType.Area);

        SimpleDmlCoord lookupCoord()
        {
            PositionComponent coord;

            if (!isArea)
            {
                coord = DmlEcs.FindEntity(world, logicLookup, entityHandle).Get<PositionComponent>();
            }
            else
            {
                var options = DmlEcs.FindEntities(world, logicLookup, entityHandle)
                    .Select(e => e.Get<PositionComponent>()).ToList();

                coord = options.ElementAt(new Random().Next(options.Count));
            }

            return new SimpleDmlCoord(coord.x, coord.y, coord.z);
        }

        void setCoord(SimpleDmlCoord coord)
        {
            if (isArea)
                throw new Exception("Cannot set the location of an area.");

            DmlEcs.FindEntity(world, logicLookup, entityHandle).Set(new PositionComponent(coord.x, coord.y, coord.z));
        }

        var managedCoord = new ManagedDmlCoord(() => lookupCoord(), v => setCoord(v));
        var bindRef = new ManagedEnvObjectReference(() => managedCoord,
            v => managedCoord.Assign(VarEnvObjectReference.CreateImmutable(v)));

        return (managedCoord.ManagedX, managedCoord.ManagedY, managedCoord.ManagedZ, bindRef);
    }

    private ManagedEnvObjectReference BindDirection(DatumHandle e)
    {
        var entityHandle = e as EntityHandle;

        if (entityHandle == null)
            throw new Exception("Data binding not supported for datum handles");

        var isArea = RuntimeTypeResolver.InheritsBaseTypeDatum(entityHandle["type"], DmlPrimitiveBaseType.Area);


        void SetDirection(object direction)
        {
            DmlEcs.FindEntity(world, logicLookup, entityHandle)
                .Set(new DirectionComponent((int)DmlEnv.AsDecimal(direction)));
        }

        EnvObjectReference GetDirection()
        {
            if (isArea)
                return EnvironmentConstants.NORTH;

            var entity = DmlEcs.FindEntity(world, logicLookup, entityHandle);

            if (!entity.Has<DirectionComponent>())
                return EnvironmentConstants.NORTH;

            return entity.Get<DirectionComponent>().Direction;
        }

        return new ManagedEnvObjectReference(GetDirection, SetDirection);
    }


    public EnvObjectReference New(Atom atm, ProcArgumentList args)
    {
        var coord = args.Get(0, defaultNull: true);

        var unpackCoord = DmlEnv.ParseCoord(coord);

        if (!coord.IsNull && unpackCoord.HasValue)
        {
            if (coord.TryGet<Atom>(out var coordAtm))
            {
                var replace =
                    RuntimeTypeResolver.InheritsBaseTypeDatum(coordAtm.type.Get<string>(), DmlPrimitiveBaseType.Turf);
                replace |= RuntimeTypeResolver.InheritsBaseTypeDatum(coordAtm.type.Get<string>(),
                    DmlPrimitiveBaseType.Area);

                atm.x.Assign(unpackCoord.Value.x);
                atm.y.Assign(unpackCoord.Value.y);
                atm.layer.Assign(unpackCoord.Value.z);

                if (replace)
                    coordAtm.ctx.Destroy(coord);
            }
            else
            {
                atm.x.Assign(unpackCoord.Value.x);
                atm.y.Assign(unpackCoord.Value.y);
            }
        }

        var zAsn = args["layer"].GetOrDefault(int.MinValue);

        if (zAsn != int.MinValue)
            atm.layer.Assign(zAsn);

        return VarEnvObjectReference.NULL;
    }
}