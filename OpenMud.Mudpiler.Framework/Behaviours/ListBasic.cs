using Antlr4.Runtime.Atn;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
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

        host.RegistedProcedures.Register(0, new ActionDatumProc("Associate",
                args => Associate(host, args[0], args[1], args[2])
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
                host.Host.Add(new DmlListItem(VarEnvObjectReference.CreateImmutable(host.Instantiate(sub.ToArray()))));
        }
        else
        {
            for (var i = 0; i < (int)size[0].Target; i++)
                host.Host.Add(new DmlListItem(VarEnvObjectReference.NULL));
        }

        return VarEnvObjectReference.Wrap(host);
    }

    private static DmlListItem[] Flatten(EnvObjectReference[] args) =>
        Flatten(args.Select(a => new DmlListItem(a)).ToArray());

    private static DmlListItem[] Flatten(DmlListItem[] args)
    {
        List<DmlListItem> f = new();

        foreach (var a in args)
            if (a.Key.Target is DmlList l)
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
            host.Host.Add(new DmlListItem(a));

        return VarEnvObjectReference.CreateImmutable(host);
    }

    private static EnvObjectReference Find(DmlList host, EnvObjectReference needleRef, EnvObjectReference firstRef,
        EnvObjectReference lastRef)
    {
        var first = firstRef.GetOrDefault(1);
        var last = lastRef.GetOrDefault(int.MaxValue);

        for (var i = first; i < last && i < host.Host.Count + 1; i++)
        {
            var isNeedle = (bool)host.ctx.op.Binary(DmlBinary.Equals, host.Host[i - 1].Key, needleRef).CompleteOrException()
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
            newList.Add(host.Host[i - 1].Key);

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

        foreach (var a in Flatten(data).ToArray())
            host.Host.Add(a);

        return VarEnvObjectReference.NULL;
    }

    private static EnvObjectReference Concat(DmlList host, params EnvObjectReference[] data)
    {
        var n = host.Instantiate();
        n.AssociativeEmplace(host.Host.ToArray());
        n.Add(data);

        return VarEnvObjectReference.CreateImmutable(n);
    }

    private static EnvObjectReference Except(DmlList host, params EnvObjectReference[] data)
    {
        var n = host.Instantiate();
        n.AssociativeEmplace(host.Host.ToArray());
        n.Remove(data);

        return VarEnvObjectReference.CreateImmutable(n);
    }

    private static EnvObjectReference RemoveSingleItem(DmlList host, EnvObjectReference o)
    {
        host.ContainsCacheDirty = true;
        var removed = false;
        for (var i = host.Host.Count - 1; i >= 0; i--)
        {
            if (host.ctx.op.Binary(DmlBinary.Equals, host.Host[i].Key, o).CompleteOrException().Get<bool>())
            {
                host.Host.RemoveAt(i);
                removed = true;
                break;
            }
        }

        return VarEnvObjectReference.CreateImmutable(removed);
    }

    private static EnvObjectReference Remove(DmlList host, params EnvObjectReference[] data)
    {
        host.ContainsCacheDirty = true;
        var removed = false;
        foreach (var item in Flatten(data))
            removed |= RemoveSingleItem(host, item.Key).Get<bool>();

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
            var i = e.Key.Target as Atom;

            if (i != null)
                host.ctx.op.Binary(DmlBinary.BitShiftLeft, e.Key, message);
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
        int idx;
        var isKeyedIndex = !DmlEnv.IsNumericType(rawIdx.Target);

        if (isKeyedIndex)
            idx = Find(host, rawIdx, VarEnvObjectReference.NULL, VarEnvObjectReference.NULL).Get<int>() - 1;
        else
            idx = rawIdx.Get<int>() - 1;

        if (idx < 0)
            return VarEnvObjectReference.NULL;

        var itm = host.Host[idx];

        return new VarEnvObjectReference(isKeyedIndex ? itm.Value : itm.Key);
    }

    private static EnvObjectReference ContainsOperator(DmlList host, EnvObjectReference subject)
    {
        if (host.ContainsCacheDirty)
        {
            host.ContainsCacheDirty = false;
            host.ContainsCache = host.Host.Select(h => h.Key).Select(VarEnvObjectReference.CreateImmutable).ToHashSet();
        }

        return VarEnvObjectReference.CreateImmutable(host.ContainsCache.Contains(subject));
    }

    private static EnvObjectReference Assign(DmlList host, EnvObjectReference rawIdx, EnvObjectReference asn)
    {
        if (rawIdx.IsNull)
            throw new Exception("Invalid index");

        if (!DmlEnv.IsNumericType(rawIdx.Target))
            return VarEnvObjectReference.CreateImmutable(host.Associate(rawIdx, asn));

        host.ContainsCacheDirty = true;
        //Lists in DreamMaker are 1 based
        var idx = rawIdx.Get<int>() - 1;

        host.Host[idx] = new DmlListItem(asn);

        return VarEnvObjectReference.CreateImmutable(host.Host[idx].Key);
    }

    private static EnvObjectReference Associate(DmlList host, EnvObjectReference keyIdx, EnvObjectReference asn, EnvObjectReference resolveCollision)
    {
        host.ContainsCacheDirty = true;
        //Lists in DreamMaker are 1 based
        var collisions = resolveCollision.TryGetOrDefault<bool>(false);
        var itm = new DmlListItem(keyIdx, asn);

        var idx = -1;
        if (collisions)
            idx = Find(host, keyIdx, VarEnvObjectReference.NULL, VarEnvObjectReference.NULL).Get<int>() - 1;
        
        if (idx < 0)
            host.Host.Add(itm);
        else
            host.Host[idx] = itm;
        
        return VarEnvObjectReference.CreateImmutable(itm.Key);
    }

    private static IEnumerator<EnvObjectReference> GetEnumerator(DmlList host)
    {
        var listClone = host.Host.Select(h => h.Key).Select(VarEnvObjectReference.CreateImmutable).ToList();
        return Enumerable.Range(0, listClone.Count).Select(i => VarEnvObjectReference.CreateImmutable(listClone[i]))
            .GetEnumerator();
    }
}