using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

public delegate EnvObjectReference ObjectInstantiator(Type type, ProcArgumentList? arguments = null);

public delegate void ObjectDestructor(EnvObjectReference target);

public delegate EnvObjectReference[] DiscoverInstancesOf(Type type);

public class DatumExecutionContext
{
    private readonly ObjectDestructor destructor;
    private readonly DiscoverInstancesOf discover;
    public readonly ITransactionProcessor ex;
    public readonly dynamic global;
    private readonly ObjectInstantiator instantiator;

    public readonly IOpSolver op;
    private readonly IDmlTaskScheduler scheduler;

    private readonly ITypeSolver typeSolver;

    public DatumExecutionContext(Datum? global, ITransactionProcessor execCtx, IOpSolver op, ITypeSolver typeSolver,
        ObjectInstantiator instantiator, ObjectDestructor destructor, DiscoverInstancesOf discover,
        IDmlTaskScheduler taskScheduler)
    {
        this.global = VarEnvObjectReference.CreateImmutable(global);
        ex = execCtx;
        this.op = op;
        this.typeSolver = typeSolver;
        this.instantiator = instantiator;
        this.destructor = destructor;
        this.discover = discover;
        scheduler = taskScheduler;
    }

    public dynamic EnumerateInstancesOf(EnvObjectReference typeName)
    {
        var t = ResolveType(typeName).Get<Type>();

        var hostList = ((EnvObjectReference)NewAtomic("/list")).Get<DmlList>();

        foreach (var e in discover(t))
            hostList.Add((EnvObjectReference)e);

        return VarEnvObjectReference.CreateImmutable(hostList);
    }

    public dynamic ResolveType(EnvObjectReference name, EnvObjectReference maxDeclOrder = null)
    {
        if (maxDeclOrder == null)
            maxDeclOrder = VarEnvObjectReference.NULL;

        var declOrder = maxDeclOrder.GetOrDefault(int.MaxValue);

        if (typeof(Type).IsAssignableFrom(name.Type))
            return VarEnvObjectReference.CreateImmutable(name);

        return VarEnvObjectReference.CreateImmutable(typeSolver.Lookup(name.Get<string>(), declOrder));
    }

    public dynamic NewAtomic(EnvObjectReference className, ProcArgumentList? arguments = null)
    {
        Type type;

        if (typeof(string).IsAssignableFrom(className.Type))
            type = typeSolver.Lookup(className.Get<string>());
        else
            type = className.Get<Type>();

        return instantiator(type, arguments);
    }

    public dynamic ListLiteral(ProcArgumentList args)
    {
        var l = instantiator(typeSolver.Lookup("/list"));
        l.Get<DmlList>().Emplace(args.GetArgumentList());

        return l;
    }

    public void Destroy(EnvObjectReference target)
    {
        destructor(target);
    }

    public EnvObjectReference LoadResource(EnvObjectReference resource)
    {
        //return global.LoadResource(resource);
        return VarEnvObjectReference.CreateImmutable(resource.Get<string>());
    }

    public Dictionary<T, EnvObjectReference[]> FindContainers<T>(EnvObjectReference[] envObjectReferences)
        where T : DmlList
    {
        var allInstanceOfContainer = discover(typeof(T));

        var discovered = new List<(T, EnvObjectReference)>();

        foreach (var i in allInstanceOfContainer)
        {
            var target = (DmlList)i.Target;

            var contained = target.ComputeIntersection(envObjectReferences);

            foreach (var c in contained)
                discovered.Add(((T)target, c));
        }

        return discovered.GroupBy(x => x.Item1).ToDictionary(x => x.Key, x => x.Select(n => n.Item2).ToArray());
    }

    public void DeferExecution(DatumProcExecutionContext datumProcExecutionContext, int lengthMilliseconds)
    {
        scheduler.DeferExecution(datumProcExecutionContext, lengthMilliseconds);
    }

    public void ClearExecutionDeferal(DatumProcExecutionContext datumProcExecutionContext)
    {
        scheduler.ClearDeferExecution(datumProcExecutionContext);
    }
}