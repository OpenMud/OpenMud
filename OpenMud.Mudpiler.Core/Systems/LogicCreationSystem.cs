using System.Diagnostics;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Core.Systems;

//Handles atom creation requests from the Runtime as well as initializing and creating logic for all atoms.
//This is different from AtomicCreationSystem which just bootstraps (invokes IMudEntityBuilder) for atoms created
//outside of the OpenMud runtime.
[With(typeof(CreateLogicComponent))]
public class LogicCreationSystem : AEntitySetSystem<float>
{
    private static readonly Dictionary<DmlPrimitive, Action<Entity>> BaseTypeMarkers = new()
    {
        { DmlPrimitive.Turf, e => e.Set<TurfComponent>() },
        { DmlPrimitive.Atom, e => e.Set<AtomicComponent>() },
        { DmlPrimitive.Obj, e => e.Set<ObjComponent>() },
        { DmlPrimitive.Area, e => e.Set(new AreaComponent()) },
        { DmlPrimitive.Movable, e => e.Set(new MovableComponent()) },
        { DmlPrimitive.Datum, e => e.Set(new DatumComponent()) },
        { DmlPrimitive.Mob, e => e.Set(new MobComponent()) }
    };

    private readonly IMudEntityBuilder entityBuilder;

    private readonly MudEnvironment environment;

    private readonly LogicDirectory logicDirectory;

    private readonly Stack<EntityCommandReceiver> currentEntity = new();

    private Guid logicInstanceIdRequest = Guid.Empty;

    private EntityCommandReceiver seedEntity;

    public LogicCreationSystem(World ecsWorld, LogicDirectory logicDirectory, IMudEntityBuilder entityBuilder,
        MudEnvironment environment, bool useBuffer = false) : base(ecsWorld, useBuffer)
    {
        this.logicDirectory = logicDirectory;
        this.environment = environment;
        this.entityBuilder = entityBuilder;
        this.environment.OnCreated += OnCreated;
        this.environment.OnDestroyed += OnDestroyed;
        this.environment.OnCreationPreinit += OnCreatePreinit;
    }

    private void OnCreatePreinit(EntityHandle logic, string className)
    {
        if (seedEntity != null)
        {
            currentEntity.Push(seedEntity);
            seedEntity = null;
        }
        else
        {
            var e = World.CreateEntity();
            currentEntity.Push(cmd => cmd(e));
            entityBuilder.CreateAtomic(e, className);
        }

        var logicInstanceId = logicDirectory.Register(logicInstanceIdRequest, logic);
        logicInstanceIdRequest = Guid.Empty;
        currentEntity.Peek()(entity => entity.Set(new LogicIdentifierComponent(logicInstanceId)));
    }

    private void HandleLogicCreation(EntityHandle logic)
    {
        var typePath = logic.Unwrap<Datum>().type.Get<string>();

        //Since created by the logic system, no need to request a new logic instance be created.
        currentEntity.Peek()(entity =>
        {
            entity.Remove<CreateLogicComponent>();
            entity.Set(new VerbDetailsComponent());

            foreach (var bt in DmlPath.EnumerateBaseTypes(typePath))
                if (BaseTypeMarkers.TryGetValue(bt, out var marker))
                    marker(entity);

            entity.Set<LogicCreatedComponent>();
        });
    }

    private void OnCreated(EntityHandle logic, string className)
    {
        HandleLogicCreation(logic);

        currentEntity.Pop();
    }

    private void OnDestroyed(EntityHandle e)
    {
        var logicInstanceId = logicDirectory[e];

        var entities = World.Where(e =>
            e.Has<LogicIdentifierComponent>() && e.Get<LogicIdentifierComponent>().LogicInstanceId == logicInstanceId);

        foreach (var found in entities)
            found.Dispose();

        logicDirectory.Remove(logicInstanceId);
    }

    public override void Dispose()
    {
        environment.OnCreated -= OnCreated;
        environment.OnDestroyed -= OnDestroyed;
        base.Dispose();
    }

    protected override void Update(float state, ReadOnlySpan<Entity> entities)
    {
        foreach (var entity in entities)
        {
            if (!entity.Has<CreateLogicComponent>())
                continue;

            var request = entity.Get<CreateLogicComponent>();

            logicInstanceIdRequest = entity.Has<LogicIdentifierComponent>()
                ? entity.Get<LogicIdentifierComponent>().LogicInstanceId
                : Guid.Empty;

            if (logicInstanceIdRequest != Guid.Empty && logicDirectory.Contains(logicInstanceIdRequest))
            {
                var logic = logicDirectory[logicInstanceIdRequest];

                currentEntity.Push(cmd => cmd(entity));
                OnCreated(logic, request.ClassName);

                logicInstanceIdRequest = Guid.Empty;
            }
            else
            {
                seedEntity = cmd => cmd(entity);
                var eh = environment.CreateAtomic(request.ClassName);

                if (entity.Has<LogicFieldInitializerComponent>())
                    foreach (var (k, v) in entity.Get<LogicFieldInitializerComponent>().FieldInitializers)
                        eh[k] = v;
            }

            entity.Remove<LogicFieldInitializerComponent>();
            entity.Remove<CreateLogicComponent>();
            Debug.Assert(currentEntity.Count == 0);

            //Put all additional logic under HandleLogicCreation,
            //this ensures logic executes even for instances created with DML process vs just externally.

            base.Update(state, entity);
        }
    }

    private delegate void EntityCommand(Entity e);

    private delegate void EntityCommandReceiver(EntityCommand cmd);
}