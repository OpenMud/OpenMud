using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

public class ListBasic : IRuntimeTypeBuilder
{
    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(target, DmlPrimitiveBaseType.List);
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        var host = (DmlList)datum;

        //Technically this doesn't make too much sense, since the methods are being statically bound to the DmlList instance, and ignoring the self argument.
        //But it is a shortcut we can probably take without much issue.
        host.RegistedProcedures.Register(0, new ActionDatumProc("New", args => New(host, args)));
        host.RegistedProcedures.Register(0, new ActionDatumProc("Emplace", args => Emplace(host, args)));
        host.RegistedProcedures.Register(0, new ActionDatumProc("Find", args => Find(host, args[0], args[1], args[2])));
        host.RegistedProcedures.Register(0, new ActionDatumProc("Copy", args => Copy(host, args[0], args[1])));
        host.RegistedProcedures.Register(0, new ActionDatumProc("Cut", args => Cut(host, args[0], args[1])));
        host.RegistedProcedures.Register(0, new ActionDatumProc("Add", args => Add(host, args.GetArgumentList())));
        host.RegistedProcedures.Register(0, new ActionDatumProc("Concat", args => Add(host, args.GetArgumentList())));
        host.RegistedProcedures.Register(0,
            new ActionDatumProc("Except", args => Except(host, args.GetArgumentList())));
        host.RegistedProcedures.Register(0,
            new ActionDatumProc("Remove", args => Remove(host, args.GetArgumentList())));

        host.RegistedProcedures.Register(0, new ActionDatumProc("RemoveOperator",
                new[] { new BinOpAsnOverride(DmlBinaryAssignment.AugSubtraction) },
                args => RemoveOperator(host, args.GetArgumentList())
            )
        );
        host.RegistedProcedures.Register(0, new ActionDatumProc("AddOperator",
                new[] { new BinOpAsnOverride(DmlBinaryAssignment.AugAddition) },
                args => AddOperator(host, args.GetArgumentList())
            )
        );

        host.RegistedProcedures.Register(0, new ActionDatumProc("BroadcastWrite",
                new[] { new BinOpOverride(DmlBinary.BitShiftLeft) },
                args => BroadcastWrite(host, args[0])
            )
        );
        host.RegistedProcedures.Register(0, new ActionDatumProc("ConcatOperator",
                new[] { new BinOpOverride(DmlBinary.Addition) },
                args => ConcatOperator(host, args.GetArgumentList())
            )
        );
        host.RegistedProcedures.Register(0, new ActionDatumProc("ExceptOperator",
                new[] { new BinOpOverride(DmlBinary.Subtraction) },
                args => ExceptOperator(host, args.GetArgumentList())
            )
        );
        host.RegistedProcedures.Register(0, new ActionDatumProc("Index",
                new[] { new BinOpOverride(DmlBinary.ArrayIndex) },
                args => IndexOperator(host, args[0])
            )
        );

        host.RegistedProcedures.Register(0, new ActionDatumProc("Contains",
                new[] { new BinOpOverride(DmlBinary.In) },
                args => ContainsOperator(host, args[0])
            )
        );

        host.RegistedProcedures.Register(0, new ActionDatumProc("AssignOperator",
                new[]
                {
                    new TernOpAsnpOverride(DmlTernery.ArrayEmplaceCopyInto),
                    new TernOpAsnpOverride(DmlTernery.ArrayEmplace)
                },
                args => Assign(host, args[0], args[1])
            )
        );
    }

    private static EnvObjectReference New(DmlList host, ProcArgumentList size)
    {
        if (size.MaxPositionalArgument == 0)
            return VarEnvObjectReference.Wrap(host);

        //Is a multi-dim array
        if (size.MaxPositionalArgument > 1)
        {
            var sub = size.GetArgumentList().Skip(1).ToList();
            for (var i = 0; i < (int)size[0].Target; i++)
                host.Host.Add(VarEnvObjectReference.CreateImmutable(host.Instantiate(sub.ToArray())));
        }
        else
        {
            for (var i = 0; i < (int)size[0].Target; i++)
                host.Host.Add(null);
        }

        return VarEnvObjectReference.Wrap(host);
    }

    private static EnvObjectReference[] Flatten(EnvObjectReference[] args)
    {
        List<EnvObjectReference> f = new();

        foreach (var a in args)
            if (a.Target is DmlList l)
                f.AddRange(l.Host);
            else
                f.Add(a);

        return f.ToArray();
    }

    private static EnvObjectReference Emplace(DmlList host, ProcArgumentList args)
    {
        host.ContainsCacheDirty = true;
        host.Host.Clear();

        foreach (var a in args.GetArgumentList())
            host.Host.Add(a);

        return VarEnvObjectReference.CreateImmutable(host);
    }

    private static EnvObjectReference Find(DmlList host, EnvObjectReference needleRef, EnvObjectReference firstRef,
        EnvObjectReference lastRef)
    {
        var first = firstRef.GetOrDefault(1);
        var last = lastRef.GetOrDefault(int.MaxValue);

        for (var i = first; i < last && i < host.Host.Count + 1; i++)
        {
            var isNeedle = (bool)host.ctx.op.Binary(DmlBinary.Equals, host.Host[i - 1], needleRef).CompleteOrException()
                .Target;

            if (isNeedle)
                return new VarEnvObjectReference(i, true);
        }

        return new VarEnvObjectReference(0, true);
    }

    private static EnvObjectReference Copy(DmlList host, EnvObjectReference firstRef, EnvObjectReference lastRef)
    {
        var first = firstRef.GetOrDefault(1);
        var last = lastRef.GetOrDefault(int.MaxValue);

        var newList = host.Instantiate();

        for (var i = first; i < last && i < host.Host.Count + 1; i++)
            newList.Add(host.Host[i - 1]);

        return VarEnvObjectReference.CreateImmutable(newList);
    }

    private static EnvObjectReference Cut(DmlList host, EnvObjectReference firstRef, EnvObjectReference lastRef)
    {
        host.ContainsCacheDirty = true;

        var first = firstRef.GetOrDefault(1);
        var last = lastRef.GetOrDefault(int.MaxValue);

        for (var i = Math.Min(host.Host.Count + 1, last) - 1; i >= first; i--)
            host.Host.RemoveAt(i - 1);

        return VarEnvObjectReference.CreateImmutable(host);
    }

    private static EnvObjectReference Add(DmlList host, params EnvObjectReference[] data)
    {
        host.ContainsCacheDirty = true;

        foreach (var a in Flatten(data))
            host.Host.Add(a);

        return VarEnvObjectReference.NULL;
    }

    private static EnvObjectReference Concat(DmlList host, params EnvObjectReference[] data)
    {
        var n = host.Instantiate();
        n.Emplace(host.Host.ToArray());
        n.Add(data);

        return VarEnvObjectReference.CreateImmutable(n);
    }

    private static EnvObjectReference Except(DmlList host, params EnvObjectReference[] data)
    {
        var n = host.Instantiate();
        n.Emplace(host.Host.ToArray());
        n.Remove(data);

        return VarEnvObjectReference.CreateImmutable(n);
    }

    private static EnvObjectReference RemoveSingleItem(DmlList host, EnvObjectReference o)
    {
        host.ContainsCacheDirty = true;
        var removed = false;
        for (var i = host.Host.Count - 1; i >= 0; i--)
            if (host.ctx.op.Binary(DmlBinary.Equals, host.Host[i], o).CompleteOrException().Get<bool>())
            {
                host.Host.RemoveAt(i);
                removed = true;
                break;
            }

        return VarEnvObjectReference.CreateImmutable(removed);
    }

    private static EnvObjectReference Remove(DmlList host, params EnvObjectReference[] data)
    {
        host.ContainsCacheDirty = true;
        var removed = false;
        foreach (var item in Flatten(data))
            removed |= RemoveSingleItem(host, item).Get<bool>();

        return VarEnvObjectReference.CreateImmutable(removed ? 1 : 0);
    }

    private static EnvObjectReference RemoveOperator(DmlList host, params EnvObjectReference[] data)
    {
        host.ContainsCacheDirty = true;
        Remove(host, data);

        return VarEnvObjectReference.CreateImmutable(host);
    }

    private static EnvObjectReference AddOperator(DmlList host, params EnvObjectReference[] data)
    {
        host.ContainsCacheDirty = true;

        Add(host, data);
        return VarEnvObjectReference.CreateImmutable(host);
    }

    private static EnvObjectReference BroadcastWrite(DmlList host, EnvObjectReference message)
    {
        foreach (var e in host.Host.ToList())
        {
            var i = e.Target as Atom;

            if (i != null)
                host.ctx.op.Binary(DmlBinary.BitShiftLeft, e, message);
        }

        return VarEnvObjectReference.NULL;
    }

    private static EnvObjectReference ConcatOperator(DmlList host, params EnvObjectReference[] data)
    {
        return Concat(host, data);
    }

    private static EnvObjectReference ExceptOperator(DmlList host, params EnvObjectReference[] data)
    {
        return Except(host, data);
    }

    private static EnvObjectReference IndexOperator(DmlList host, EnvObjectReference rawIdx)
    {
        var idx = rawIdx.Get<int>() - 1;

        return new VarEnvObjectReference(host.Host[idx]);
    }

    private static EnvObjectReference ContainsOperator(DmlList host, EnvObjectReference subject)
    {
        if (host.ContainsCacheDirty)
        {
            host.ContainsCacheDirty = false;
            host.ContainsCache = host.Host.Select(VarEnvObjectReference.CreateImmutable).ToHashSet();
        }

        return VarEnvObjectReference.CreateImmutable(host.ContainsCache.Contains(subject));
    }

    private static dynamic Assign(DmlList host, EnvObjectReference rawIdx, EnvObjectReference asn)
    {
        host.ContainsCacheDirty = true;
        //Lists in DreamMaker are 1 based
        var idx = rawIdx.Get<int>() - 1;

        host.Host[idx] = asn;

        return VarEnvObjectReference.CreateImmutable(host.Host[idx]);
    }

    private static IEnumerator<EnvObjectReference> GetEnumerator(DmlList host)
    {
        var listClone = host.Host.Select(VarEnvObjectReference.CreateImmutable).ToList();
        return Enumerable.Range(0, listClone.Count).Select(i => VarEnvObjectReference.CreateImmutable(listClone[i]))
            .GetEnumerator();
    }
}