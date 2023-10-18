using DefaultEcs;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.Framework.Behaviours;
using OpenMud.Mudpiler.Framework.Datums;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework;

public class BaseDmlFramework : IDmlFramework
{
    private readonly IEntityVisibilitySolver entityVisibilitySolver;
    private readonly LogicDirectory logicLookup;
    private readonly World world;

    public BaseDmlFramework(World world, LogicDirectory logicLookup, IDmlTaskScheduler taskScheduler,
        IEntityVisibilitySolver entityVisibilitySolver)
    {
        this.world = world;
        this.logicLookup = logicLookup;
        Scheduler = taskScheduler;
        this.entityVisibilitySolver = entityVisibilitySolver;

        Types = new[]
        {
            new DmlRuntimeTypeDescriptor(
                "/list",
                typeof(DatumDmlList)
            ),
            new DmlRuntimeTypeDescriptor(
                "/list/verb",
                typeof(DmlVerbList)
            ),
            new DmlRuntimeTypeDescriptor(
                "/list/exclusive",
                typeof(DmlExclusiveContentsList)
            ),
            new DmlRuntimeTypeDescriptor(
                "/primitive_coord",
                typeof(VarDmlCoord)
            ),
            new DmlRuntimeTypeDescriptor(
                "/sound",
                typeof(SoundInfo)
            )
        };
    }

    public BaseDmlFramework() : this(new World(), new LogicDirectory(), new NullScheduler(), new NullVisibilitySolver())
    {
    }

    public IDmlTaskScheduler Scheduler { get; }
    public DmlRuntimeTypeDescriptor[] Types { get; }

    public IRuntimeTypeBuilder[] CreateBuilders(ITypeSolver typeSolver, ObjectInstantiator instantiator)
    {
        return new IRuntimeTypeBuilder[]
        {
            new GlobalTyping(typeSolver, instantiator),
            new GlobalPathing(world, logicLookup),
            new AtomicReadWrite(logicLookup, Echo, EchoSound),
            new WorldReadWrite(Echo, EchoSound),
            new AtomicEnvironmentInteraction(world, logicLookup, instantiator, typeSolver, entityVisibilitySolver),
            new MobMovement(world, logicLookup),
            new ObjMovement(world, logicLookup),
            new AtomicCollision(),
            new MobDefaultProperties(),
            new AtomicBasic(world, logicLookup, instantiator),
            new GlobalSound(instantiator),
            new GlobalTasks(),
            new ProcBasic(),
            new ListBasic(),
            new GlobalUtil()
        };
    }

    private void EchoSound(DatumHandle subject, SoundInfo message)
    {
        Guid? scope = subject is EntityHandle e ? logicLookup[e] : null;

        var configuration = DmlEnv.AsLogical(message.repeat) ? SoundConfiguration.Loop : SoundConfiguration.Play;

        world.Publish(
            new ConfigureSoundMessage(
                scope,
                message.file.GetOrDefault<string?>(null),
                DmlEnv.AsNumeric(message.channel),
                configuration
            )
        );
    }

    private void Echo(DatumHandle subject, string message)
    {
        if (subject is EntityHandle e)
        {
            var entityId = logicLookup[e];
            world.Publish(new EntityEchoMessage(entityId, message));
        }
        else
        {
            world.Publish(new WorldEchoMessage(message));
        }
    }
}