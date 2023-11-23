using System.Reflection;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public delegate void EntityCreateEvent(EntityHandle e, string className);

public delegate void EntityEvent(EntityHandle e);

public delegate void DestroyDatumCallback(DatumHandle e, Datum d);

public class WorldPieceRepository
{
    private readonly Dictionary<EnvObjectReference, DatumHandle> backwards = new();
    private readonly Dictionary<DatumHandle, DestroyObjectCallback> deleteListeners = new();
    private readonly Dictionary<DatumHandle, EnvObjectReference> forward = new();
    private readonly Dictionary<Type, HashSet<DatumHandle>> typeCache = new();
    private readonly Dictionary<object, DatumHandle> worldPieces = new();
    public IEnumerable<DatumHandle> Actors => forward.Keys.ToList();

    public event DestroyDatumCallback? ObjectDestroyed;

    public void Set(DatumHandle k, EnvObjectReference v, IWeakDestroyRegister deleteCallback)
    {
        forward[k] = v;
        backwards[v] = k;
        worldPieces[v.Target] = k;

        var t = v.Target.GetType();

        if (!typeCache.ContainsKey(t))
            typeCache[t] = new HashSet<DatumHandle>();

        typeCache[t].Add(k);

        DestroyObjectCallback deleteListener = NotifyDeleted;
        deleteListeners[k] = deleteListener;
        deleteCallback.Subscribe(deleteListener);
    }

    public DatumHandle[] DiscoverInstancesOf(Type t)
    {
        return typeCache.Where(k => t.IsAssignableFrom(k.Key)).SelectMany(k => k.Value).ToArray();
    }

    public EnvObjectReference[] DiscoverInstancesOfRaw(Type t)
    {
        return DiscoverInstancesOf(t).Select(k => forward[k]).ToArray();
    }

    private void NotifyDeleted(object o)
    {
        if (!worldPieces.TryGetValue(o, out var hdl))
            return;

        var env = forward[hdl];
        forward.Remove(hdl);
        backwards.Remove(env);

        var t = o.GetType();

        if (typeCache.ContainsKey(t))
            typeCache[t].Remove(hdl);

        ObjectDestroyed?.Invoke(hdl, (Datum)o);

        deleteListeners.Remove(hdl);
    }

    public EnvObjectReference Lookup(DatumHandle k)
    {
        return forward[k];
    }

    public DatumHandle Lookup(EnvObjectReference k)
    {
        return backwards[k];
    }

    public bool TryLookup(DatumHandle k, out EnvObjectReference? v)
    {
        return forward.TryGetValue(k, out v);
    }

    public bool TryLookup(EnvObjectReference k, out DatumHandle? v)
    {
        return backwards.TryGetValue(k, out v);
    }
}

public sealed class MudEnvironment
{
    private readonly IRuntimeTypeBuilder[] builders;
    private readonly Type globalType;
    private readonly IOpSolver operationSolver;
    private readonly WorldPieceRepository pieces = new();
    private readonly Global rawGlobal;

    private readonly GameWorld rawWorld;

    private readonly DmlList rawWorldContents;
    private readonly IDmlTaskScheduler scheduler;

    public MudEnvironment(ITypeSolver typeSolver, IDmlFramework framework)
    {
        pieces.ObjectDestroyed += EntityBeingDeleted;
        scheduler = framework.Scheduler;
        operationSolver = new OpDirectory(SetupExecute);
        TypeSolver = typeSolver;

        builders = framework.CreateBuilders(typeSolver, CreateRawDatum);

        globalType = typeSolver.Lookup("/GLOBAL");
        Global = CreateDatum("/GLOBAL");

        rawGlobal = (Global)Unwrap(Global).Target;

        World = CreateDatum("/world");

        rawWorld = (GameWorld)Unwrap(World).Target;
        rawWorldContents = (DmlList)rawWorld.contents.Target;
        Global["world"] = rawWorld;
    }

    public DatumHandle Global { get; }
    public DatumHandle World { get; }

    public ITypeSolver TypeSolver { get; }

    public IEnumerable<DatumHandle> Actors => pieces.Actors.ToList();

    public event EntityCreateEvent OnCreationPreinit;
    public event EntityCreateEvent OnCreated;
    public event EntityEvent OnDestroyed;

    private DatumHandle ReverseLookup(EnvObjectReference p)
    {
        return pieces.Lookup(p);
    }

    private EnvObjectReference Lookup(DatumHandle p)
    {
        return pieces.Lookup(p);
    }

    //TODO: This function is doing too much. Maybe need a SystemBuilder
    public static MudEnvironment Create(Assembly projectAssembly, IDmlFramework? framework = null)
    {
        var sourceTypeLibrary = LoadAssemblyTypes(projectAssembly);
        //var runtimeTypes 
        //var runtimeTypeLibrary = LoadAssemblyTypes(Assembly.GetExecutingAssembly(), typeof(OpenMudEnvironmentRootPlaceholder).Namespace);
        var runtimeTypeLibrary = framework?.Types?.ToDictionary(
            x => Tuple.Create(x.Precedence, x.TypePath),
            x => x.Type
        );

        var typeLibrary = sourceTypeLibrary.AsEnumerable();

        if (runtimeTypeLibrary != null)
            typeLibrary = typeLibrary.Concat(runtimeTypeLibrary);

        var netTypeLibrary = typeLibrary.ToDictionary(x => x.Key, x => x.Value);

        return new MudEnvironment(new SimpleTypeSolver(netTypeLibrary), framework ?? new NullDmlFramework());
    }

    private static Dictionary<Tuple<int, string>, Type> LoadAssemblyTypes(Assembly asm, string? namespaceFilter = null)
    {
        Tuple<int, string> NormalizeClassName(object o)
        {
            //Entities cannot be redeclared, so all declarations will just have the order 0
            if (o is EntityDefinition e)
                return Tuple.Create(0, e.Name);
            if (o is ProcDefinition p)
                return Tuple.Create(p.DeclarationOrder, p.Name);
            if (o == null)
                return null;

            throw new Exception("Unknown definition type");
        }

        return
            asm.GetTypes()
                .Where(x => namespaceFilter == null || (x.Namespace != null && x.Namespace.StartsWith(namespaceFilter)))
                .SelectMany(t =>
                    t.GetCustomAttributes(typeof(EntityDefinition)).Select(f =>
                        (cls: t, attr: NormalizeClassName(f))
                    ).Concat(
                        t.GetCustomAttributes(typeof(ProcDefinition)).Select(f =>
                            (cls: t, attr: NormalizeClassName(f))
                        )
                    )
                )
                .Where(t => t.attr != null)
                .ToDictionary(
                    e => e.attr,
                    e => e.cls
                );
    }

    private DatumProcExecutionContext SetupExecute(DatumProcExecutionContext? caller, EnvObjectReference source,
        EnvObjectReference desintation, ProcArgumentList arguments, string method, long? precedence = null, bool nullIfNotFound = false)
    {
        if (desintation == null)
            throw new ArgumentNullException("Destination cannot be null.");

        Datum? usr = null;
        if (typeof(Atom).IsAssignableFrom(desintation.Type))
        {
            if (source == null || source.IsNull)
                source = desintation;
            else if (!typeof(Atom).IsAssignableFrom(source.Type))
                source = desintation;

            usr = source.Get<Datum>();
        }

        DatumProcExecutionContext ret;

        if (typeof(Datum).IsAssignableFrom(desintation.Type))
        {
            var destDatum = desintation.Get<Datum>();

            if (destDatum.HasProc(method, precedence ?? -1))
                ret = desintation.Get<Datum>().Invoke(caller, method, usr, arguments, precedence);
            else if (nullIfNotFound)
                return new PreparedDatumProcContext(() => VarEnvObjectReference.NULL);
            else
                throw new DmlRuntimeError("Procedure not found.");
        }
        else
            ret = new PreparedDatumProcContext(() =>
                new VarEnvObjectReference(desintation.Invoke(method, arguments), true));

        var netProc = new PreparedChainDatumProcContext(
            ret,
            r => new PreparedDatumProcContext(() => new VarEnvObjectReference(r, true))
        );

        return netProc;
    }

    private void SetupContext(Datum global, EnvObjectReference subjectInstance, ITransactionProcessor processor)
    {
        var newContext = new DatumExecutionContext(
            global,
            processor,
            operationSolver,
            TypeSolver,
            CreateRawDatum,
            DestroyRawDatum,
            DiscoverInstancesOfRaw,
            scheduler
        );

        ((Datum)subjectInstance.Target).SetContext(newContext);
    }

    private EnvObjectReference[] DiscoverInstancesOfRaw(Type type)
    {
        return pieces.DiscoverInstancesOfRaw(type);
    }

    private void DestroyRawDatum(EnvObjectReference target)
    {
        var handle = ReverseLookup(target);
        handle.Destroy();
    }

    public DatumHandle CreateDatum(string className, WrappedProcArgumentList? arguments = null)
    {
        var typeDefintion = TypeSolver.Lookup(className);

        return CreateDatum(typeDefintion, arguments);
    }

    public DatumHandle CreateDatum(Type typeDefintion, WrappedProcArgumentList? arguments = null)
    {
        var r = CreateRawDatum(typeDefintion, arguments?.Unwrap(Unwrap));

        return (DatumHandle)Wrap(r);
    }

    private void BuildFramework(string className, Type definition, DatumHandle handle,
        DatumProcCollection registedProcedures)
    {
        var isMethod = TypeSolver.IsMethod(definition);

        bool Test(IRuntimeTypeBuilder builder)
        {
            return isMethod ? builder.AcceptsProc(className) : builder.AcceptsDatum(className);
        }

        foreach (var builder in builders.Where(Test))
            builder.Build(handle, Unwrap(handle).Get<Datum>(), registedProcedures);
    }

    private bool IsInWorld(EnvObjectReference entity)
    {
        if (entity.TryGet<Atom>(out var atm))
            return atm.Container.Target == rawWorldContents;

        return false;
    }

    private EnvObjectReference CreateRawDatum(Type typeDefintion, ProcArgumentList? arguments)
    {
        var className = TypeSolver.LookupName(typeDefintion);

        var typeName = DmlPath.RootClassName(className);

        if (!TypeSolver.IsTypeKnown(typeName))
            throw new Exception("Unknown type: " + className);

        var entityInstance = (Datum)Activator.CreateInstance(typeDefintion);

        var destroyCallback = new WeakDestroyCallback(entityInstance);

        var entity = new VarEnvObjectReference(entityInstance, false, destroyCallback);

        var global = Global == null ? null : Lookup(Global);
        var transactionProcessor = new SimpleLogicInteractionProcessor(
            entity,
            global,
            SetupExecute
        );

        DatumHandle handle;

        var isAtomic = typeof(Atom).IsAssignableFrom(entity.Type);

        if (isAtomic)
            handle = new EntityHandle(this, scheduler, () => IsInWorld(entity), Lookup, Wrap, Unwrap, SetupExecuteVerb,
                DiscoverVerbs, transactionProcessor, destroyCallback.Destroy);
        else
            handle = new DatumHandle(this, scheduler, Lookup, Wrap, Unwrap, transactionProcessor,
                destroyCallback.Destroy);

        var globalDatum = DmlPath.IsGlobal(className) ? entityInstance : rawGlobal;

        var typePath = DmlPath.BuildQualifiedDeclarationName(className);
        entityInstance.type.Assign(typePath, true);

        if (entityInstance is Atom atm && atm.name.IsNull)
            atm.name.Assign(DmlPath.ExtractComponentName(typePath).Replace('_', ' '));

        pieces.Set(handle, entity, destroyCallback);

        SetupContext(globalDatum, entity, transactionProcessor);

        BuildFramework(className, typeDefintion, handle, entityInstance.RegistedProcedures);

        if (isAtomic)
        {
            rawWorldContents.Add(entity);

            if (OnCreationPreinit != null)
                OnCreationPreinit((EntityHandle)handle, className);
        }

        //invoke library
        entity.InvokeIfPresent("_constructor", new EnvObjectReference[0]);

        if (handle.HasProc("_constructor_fieldinit"))
            handle.ExecProc("_constructor_fieldinit").CompleteOrException();

        //Finally, invoke the New procedure (if present)
        if (handle.HasProc("New"))
            handle.ExecProc("New", arguments?.Wrap(Wrap));

        if (isAtomic)
            if (OnCreated != null)
                OnCreated((EntityHandle)handle, className);

        return entity;
    }

    //USE THIS WHEN LOGIC EQUESTS ENTITY TO BE CREATED!
    public EntityHandle CreateAtomic(string className, WrappedProcArgumentList? arguments = null)
    {
        var logic = CreateDatum(className, arguments) as EntityHandle;

        if (logic == null)
            throw new Exception("This is a datum, not an entity.");

        return logic;
    }


    //USE THIS WHEN LOGIC EQUESTS ENTITY TO BE CREATED!
    public EntityHandle CreateAtomic(string className, params object[] arguments)
    {
        return CreateAtomic(className, new WrappedProcArgumentList(arguments).Unwrap(Unwrap));
    }

    private void EntityBeingDeleted(DatumHandle e, Datum d)
    {
        rawWorldContents.Remove(VarEnvObjectReference.CreateImmutable(d));

        if (e is EntityHandle eh)
            OnDestroyed?.Invoke(eh);
    }

    public EnvObjectReference Unwrap(object a)
    {
        if (a is DatumHandle h)
            return Lookup(h);

        if (a == null)
            return VarEnvObjectReference.NULL;

        return VarEnvObjectReference.CreateImmutable(a);
    }

    public object Wrap(EnvObjectReference ret)
    {
        if (ret == null)
            return ret;

        if (ret.IsNull)
            return null;

        if (!ret.Type.IsPrimitive && !(ret.Target is string))
            if (pieces.TryLookup(ret, out var wrapped))
                return wrapped;

        return ret.Target;
    }

    private IEnumerable<VerbMetadata> DiscoverVerbs(EntityHandle target)
    {
        var targetAtomic = pieces.Lookup(target);

        return targetAtomic.Get<Datum>().DiscoverVerbs();
    }

    private DatumProcExecutionContext SetupExecuteVerb(DatumProcExecutionContext? caller, EntityHandle source,
        EntityHandle target, string verb, ProcArgumentList arguments)
    {
        var targetAtomic = pieces.Lookup(target);
        var sourceAtomic = pieces.Lookup(source);

        var ret = SetupExecute(caller, sourceAtomic, targetAtomic, arguments,
            targetAtomic.Get<Datum>().ResolveVerb(verb));

        return ret;
    }
}