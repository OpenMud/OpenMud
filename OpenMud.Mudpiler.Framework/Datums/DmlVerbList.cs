using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Datums;

public class VerbDatumProc : DatumProc
{
    public readonly DatumProc inner;

    public VerbDatumProc(DatumProc inner)
    {
        this.inner = inner;

        if (inner is VerbDatumProc)
            throw new Exception("Inner cannot be wrapped by VerbDatumProc");
    }

    public override string Name => inner.Name;

    public override IDmlProcAttribute[] Attributes()
    {
        return inner.Attributes();
    }

    public override DatumProcExecutionContext Create()
    {
        return inner.Create();
    }

    public override bool Equals(object? obj)
    {
        return obj is VerbDatumProc proc &&
               inner.GetType().IsEquivalentTo(proc.inner.GetType());
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(inner.GetType());
    }
}

public class VerbListCollectionEntry
{
    public readonly bool isNative;
    public readonly VerbMetadata metadata;
    public readonly VerbDatumProc proc;
    public long registeredPrecedent;

    public VerbListCollectionEntry(bool isNative, VerbMetadata metadata, VerbDatumProc proc)
    {
        this.metadata = metadata;
        this.proc = proc;
        this.isNative = isNative;
    }

    public override bool Equals(object? obj)
    {
        return obj is VerbListCollectionEntry entry &&
               EqualityComparer<VerbDatumProc>.Default.Equals(proc, entry.proc);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(proc);
    }
}

public class VerbListCollection : IList<DmlListItem>
{
    private DatumExecutionContext ctx;

    private VerbSrc defaultVerbSrc = VerbSrc.Default;
    private readonly ObservableCollection<VerbListCollectionEntry> internalVerbCollection = new();
    private DatumProcCollection registeredProcedures;

    public VerbListCollection()
    {
        internalVerbCollection.CollectionChanged += CollectionChanged;
    }

    public VerbMetadata[] AvailableVerbs => internalVerbCollection
        .GroupBy(v => v.proc.Name)
        .Select(g => g.OrderByDescending(m => m.registeredPrecedent).First())
        .Select(v => v.metadata)
        .Distinct().ToArray();

    public DmlListItem this[int index]
    {
        get => new DmlListItem(Decode(internalVerbCollection[index]));
        set => internalVerbCollection[index] = Encode(value.Key);
    }

    public int Count => internalVerbCollection.Count;

    public bool IsReadOnly => false;

    public void Add(DmlListItem item)
    {
        var encoded = Encode(item.Key);

        internalVerbCollection.Add(encoded);
    }

    public void Clear()
    {
        internalVerbCollection.Clear();
    }

    public bool Contains(DmlListItem item)
    {
        return internalVerbCollection.Contains(Encode(item.Key));
    }

    public void CopyTo(DmlListItem[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<DmlListItem> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(DmlListItem item)
    {
        return internalVerbCollection.IndexOf(Encode(item.Key));
    }

    public void Insert(int index, DmlListItem item)
    {
        var encoded = Encode(item.Key);

        internalVerbCollection.Insert(index, encoded);
    }

    public bool Remove(DmlListItem item)
    {
        var encoded = Encode(item.Key);

        if (!internalVerbCollection.Remove(encoded))
            return false;

        return true;
    }

    public void RemoveAt(int index)
    {
        internalVerbCollection.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return internalVerbCollection.Select(Decode).GetEnumerator();
    }

    public event Action VerbsChanged;

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void ProcessNew(VerbListCollectionEntry entry)
        {
            if (entry.isNative)
            {
                if (!registeredProcedures.IsRegistered(entry.proc.inner, out entry.registeredPrecedent))
                    throw new Exception("Native procedure is not registered.");
                return;
            }

            if (registeredProcedures.IsRegistered(entry.proc, out entry.registeredPrecedent))
                return;

            entry.registeredPrecedent = registeredProcedures.Register(entry.proc);
        }

        void ProcessRemove(VerbListCollectionEntry entry)
        {
            if (entry.isNative || !registeredProcedures.IsRegistered(entry.proc, out _))
                return;

            registeredProcedures.Unregister(entry.proc);

            entry.registeredPrecedent = -1;
        }

        if (e.OldItems != null)
            foreach (var p in e.OldItems)
                ProcessRemove((VerbListCollectionEntry)p);

        if (e.NewItems != null)
            foreach (var p in e.NewItems)
                ProcessNew((VerbListCollectionEntry)p);

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new Exception("Unsupported case.");
        }

        VerbsChanged?.Invoke();
    }

    private EnvObjectReference Decode(VerbListCollectionEntry envObjectReference)
    {
        return VarEnvObjectReference.CreateImmutable(envObjectReference.proc.inner.GetType());
    }

    private VerbListCollectionEntry EncodeFrom(DatumProc proc, bool isNative, bool deriveFromRegistered = true,
        string? nameOverride = null, string? descOverride = null)
    {
        var computedAttributes = deriveFromRegistered
            ? registeredProcedures.GetAttributes(proc.Name, overlay: proc.Attributes())
            : new List<IDmlProcAttribute>();

        IReadOnlyList<T> getMultipleAttrs<T>()
        {
            return computedAttributes.Where(x => typeof(T).IsAssignableFrom(x.GetType())).Cast<T>().ToList();
        }

        T getAttr<T>()
        {
            return (T)computedAttributes.Where(x => typeof(T).IsAssignableFrom(x.GetType())).FirstOrDefault();
        }

        var procName = proc.Name;

        var verbName = getAttr<VerbName>()?.Name;

        if (verbName == null)
            verbName = procName;

        if (nameOverride != null)
            verbName = nameOverride;

        var desc = descOverride ?? getAttr<VerbDescription>()?.Desc;
        var categoryName = getAttr<VerbCategory>()?.Name;
        var source = getAttr<VerbSrc>() ?? defaultVerbSrc;

        var argAsConstraint = getMultipleAttrs<ArgAsConstraint>();
        var srcConstraint = getMultipleAttrs<SimpleSourceConstraint>();
        var listEvalSourceConstraint = getMultipleAttrs<ListEvalSourceConstraint>();

        return new VerbListCollectionEntry(isNative,
            new VerbMetadata(procName, verbName, categoryName, desc ?? "", source, argAsConstraint, srcConstraint,
                listEvalSourceConstraint), new VerbDatumProc(proc));
    }

    private VerbListCollectionEntry Encode(EnvObjectReference envObjectReference, bool isNative = false)
    {
        var t = envObjectReference.Target;

        if (t is VerbListCollectionEntry e) return e;

        if (t is DatumProc p) return EncodeFrom(p, isNative);

        if (t is Type)
        {
            var proc = ((EnvObjectReference)ctx.NewAtomic(envObjectReference)).Get<DatumProc>();

            return EncodeFrom(proc, isNative);
        }

        throw new Exception("Cannot add this to a verb list.");
    }

    public void AddOrigin(EnvObjectReference item)
    {
        var encoded = Encode(item, true);

        internalVerbCollection.Add(encoded);
    }

    internal void SetRegisteredProcedures(DatumProcCollection registedProcedures)
    {
        registeredProcedures = registedProcedures;
    }

    internal void SetContext(DatumExecutionContext ctx)
    {
        this.ctx = ctx;
    }

    internal void Register(DatumProc d, string? name, string? description)
    {
        var e = EncodeFrom(d, false, false, name ?? d.Name, description);

        //var e = new VerbListCollectionEntry(false, new VerbMetadata(d.Name, name ?? d.Name, null, description, VerbSrc.Default), new VerbDatumProc(d));
        internalVerbCollection.Add(e);
    }

    internal void SetDefaultVerbSource(VerbSrc verbSrc)
    {
        defaultVerbSrc = verbSrc;
    }
}

public class DmlVerbList : AbstractDmlVerbList
{
    private readonly VerbListCollection verbList = new();
    public override IList<DmlListItem> Host => verbList;

    public override VerbMetadata[] AvailableVerbs => verbList.AvailableVerbs;

    public override Action? Changed { get; set; }


    public override void AddOrigin(EnvObjectReference verb)
    {
        verbList.AddOrigin(verb);
    }

    public new void _constructor()
    {
        base._constructor();
        verbList.SetContext(ctx);

        verbList.VerbsChanged += () => Changed?.Invoke();
    }

    public override void SetDefaultVerbSource(VerbSrc verbSrc)
    {
        verbList.SetDefaultVerbSource(verbSrc);
    }

    public override void SetRegisteredProcedures(DatumProcCollection registedProcedures)
    {
        verbList.SetRegisteredProcedures(registedProcedures);
    }

    public override void Register(DatumProc d, string? name, string? description)
    {
        verbList.Register(d, name, description);
    }
}