using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public static class DmlListHelper
{
    public static DmlList Instantiate(this DmlList h, params EnvObjectReference[] args)
    {
        return ((EnvObjectReference)h.ctx.NewAtomic(VarEnvObjectReference.CreateImmutable(h.GetType()),
            new ProcArgumentList(args))).Get<DmlList>();
    }

    public static void Add(this DmlList h, params EnvObjectReference[] o)
    {
        h.Invoke(null, "Add", null, new ProcArgumentList(o)).CompleteOrException();
    }

    public static void Remove(this DmlList h, EnvObjectReference o)
    {
        h.Invoke(null, "Remove", null, new ProcArgumentList(o)).CompleteOrException();
    }

    public static void Remove(this DmlList h, IEnumerable<EnvObjectReference> o)
    {
        h.Invoke(null, "Remove", null, new ProcArgumentList(o.ToArray())).CompleteOrException();
    }

    public static DmlList Emplace(this DmlList h, IEnumerable<EnvObjectReference> o)
    {
        h.Invoke(null, "Emplace", null, new ProcArgumentList(o.ToArray())).CompleteOrException();

        return h;
    }

    public static IEnumerable<EnvObjectReference> ComputeIntersection(this DmlList h, IEnumerable<EnvObjectReference> o)
    {
        if (!h.ContainsCacheDirty)
            return h.ContainsCache.Intersect(o);

        return h.Host.Intersect(o);
    }

    public static bool Any(this DmlList h)
    {
        return h.Host.Any();
    }

    public static EnvObjectReference First(this DmlList h)
    {
        return h.Host.First();
    }

    public static bool Contains(this DmlList h, EnvObjectReference o)
    {
        if (!h.ContainsCacheDirty)
            return h.ContainsCache.Contains(o);

        var r = h.Invoke(null, "Contains", null, new ProcArgumentList(o)).CompleteOrException();

        return r.Get<bool>();
    }
}

public abstract class DmlList : Datum
{
    public abstract IList<EnvObjectReference> Host { get; }

    public HashSet<EnvObjectReference> ContainsCache { get; set; } = new();

    public bool ContainsCacheDirty { get; set; } = false;

    public int len => Host.Count;

    public abstract Action? Changed { get; set; }

    public new void _constructor()
    {
        base._constructor();
    }
}