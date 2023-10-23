using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public struct ProcMetadata
{
    public readonly string Name;
    public readonly DatumProc Proc;
    public readonly IDmlProcAttribute[] Attributes;

    public ProcMetadata(string name, DatumProc proc, IDmlProcAttribute[] attributesOrderedByPrec)
    {
        Name = name;
        Proc = proc;
        Attributes = attributesOrderedByPrec;
    }


    public bool HasAttribute<T>() where T : IDmlProcAttribute
    {
        return GetAttributes<T>().Any();
    }

    internal T[] GetAttributes<T>() where T : IDmlProcAttribute
    {
        return Attributes.Where(x => typeof(T).IsAssignableFrom(x.GetType())).Cast<T>().ToArray();
    }
}

public sealed class DatumProcCollection
{
    private readonly Dictionary<long, Dictionary<string, DatumProc>> procedures = new();

    public void Register(long precedence, DatumProc proc)
    {
        if (!procedures.ContainsKey(precedence))
            procedures[precedence] = new Dictionary<string, DatumProc>();

        var prec = procedures[precedence];

        if (prec.ContainsKey(proc.Name))
            throw new Exception("Method with the same name already registered on this precedent.");

        prec[proc.Name] = proc;
    }

    public long Register(DatumProc proc)
    {
        var prec = procedures.Where(x => !x.Value.ContainsKey(proc.Name)).Select(x => x.Key).Append(-1).Max();

        if (prec < 0)
            prec = procedures.Keys.Max() + 1;

        Register(prec, proc);

        return prec;
    }

    public bool IsRegistered(DatumProc proc, out long prec)
    {
        prec = procedures.Where(x => x.Value.Values.Contains(proc)).Select(x => x.Key).Append(-1).Max();

        return prec >= 0;
    }

    public void Unregister(DatumProc t)
    {
        foreach (var v in procedures.Values)
        {
            var remove = v.Where(x => x.Value.Equals(t)).Select(x => x.Key).FirstOrDefault();
            if (remove != null)
                v.Remove(remove);
        }
    }

    public DatumProcExecutionContext Invoke(DatumProcExecutionContext? caller, Datum? usr, Datum self,
        DatumExecutionContext ctx, string name, ProcArgumentList args, long? prec = null)
    {
        if (!procedures.Any())
            throw new Exception("No procedure exist in collection.");

        var maxPrec = procedures.Keys.Max();

        if (prec.HasValue)
            maxPrec = Math.Min(prec.Value, maxPrec);

        var levels = procedures.Keys.OrderByDescending(x => x).Where(x => x >= 0 && x <= maxPrec);
        foreach (var i in levels)
        {
            var procs = procedures[i];

            if (procs.TryGetValue(name, out var subjectProc))
            {
                var invokeCtx = subjectProc.Create().SetupContext(caller, usr, self, ctx);
                invokeCtx.precedence = i;
                invokeCtx.ActiveArguments = subjectProc.DefaultArgumentList().Overlay(args);

                return invokeCtx;
            }
        }

        throw new Exception("Procedure doesn't exist in collection.");
    }

    private static List<IDmlProcAttribute> CoalesceAttributes(
        IEnumerable<IDmlProcAttribute> attributesSortedByPrecedence)
    {
        Dictionary<Type, ProcAttributeCoalesceStrategy> attrsTypes = new();

        List<IDmlProcAttribute> accepted = new();

        foreach (var a in attributesSortedByPrecedence)
        {
            var t = a.GetType();

            if (!attrsTypes.TryGetValue(t, out var coalesceStrat))
            {
                attrsTypes[t] = a.CoalesceStrategy;
                accepted.Add(a);
            }
            else
            {
                switch (coalesceStrat)
                {
                    case ProcAttributeCoalesceStrategy.Replace:
                        continue;
                    case ProcAttributeCoalesceStrategy.Concat:
                        accepted.Add(a);
                        break;
                    default:
                        throw new Exception("Unknown coalesce strategy.");
                }
            }
        }

        return accepted;
    }

    private List<IDmlProcAttribute> CompileAttributes(string name, long prec)
    {
        var attr = new List<IDmlProcAttribute>();

        if (!procedures.Any())
            return attr;

        var maxPrec = Math.Max(prec, procedures.Keys.Max());

        var levels = procedures.Keys.OrderByDescending(x => x).Where(x => x >= 0 && x <= maxPrec);
        foreach (var i in levels)
        {
            var procs = procedures[i];
            if (procs.TryGetValue(name, out var c)) attr.AddRange(c.Attributes());
        }

        return CoalesceAttributes(attr);
    }

    public IEnumerable<ProcMetadata> Enumerate(long startingPrec = -1)
    {
        if (procedures.Any())
        {
            var maxPrec = procedures.Keys.Max();
            var explored = new HashSet<string>();

            if (startingPrec >= 0)
                maxPrec = Math.Max(maxPrec, startingPrec);

            var levels = procedures.Keys.OrderByDescending(x => x).Where(x => x >= 0 && x <= maxPrec);
            foreach (var i in levels)
            {
                var procs = procedures[i];
                foreach (var (name, proc) in procs)
                    //Only want to return toplevel methods
                    if (explored.Add(name))
                        yield return new ProcMetadata(name, proc, CompileAttributes(name, i).ToArray());
            }
        }
    }

    public List<IDmlProcAttribute> GetAttributes(string name, long? prec = null,
        IEnumerable<IDmlProcAttribute>? overlay = null)
    {
        var attrs = CompileAttributes(name, prec ?? procedures.Keys.Append(0).Max());

        if (overlay == null)
            return attrs;

        return CoalesceAttributes(overlay.Concat(attrs));
    }
}