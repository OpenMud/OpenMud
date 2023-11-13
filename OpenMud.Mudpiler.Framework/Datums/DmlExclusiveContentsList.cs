using System.Collections.ObjectModel;
using System.Collections.Specialized;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Datums;

public sealed class DmlExclusiveContentsList : DmlList
{
    private readonly ObservableCollection<DmlListItem> host = new();

    public DmlExclusiveContentsList()
    {
        host.CollectionChanged += HostChanged;
    }

    public override IList<DmlListItem> Host => host;

    public override Action? Changed { get; set; }

    private void HostChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null || !this.Any())
        {
            Changed?.Invoke();
            return;
        }

        var containers =
            ctx.FindContainers<DmlExclusiveContentsList>(e.NewItems.Cast<DmlListItem>().Select(x => x.Key).Distinct().ToArray());

        foreach (var (c, items) in containers)
        {
            if (c == this)
                continue;

            c.Remove(items);
        }

        foreach (var itm in e.NewItems)
        {
            var atmRef = ((DmlListItem)itm).Key;
            if (!atmRef.IsNull && atmRef.TryGet<Atom>(out var atm))
                atm.Container.Assign(VarEnvObjectReference.CreateImmutable(this));
        }
        Changed?.Invoke();
    }
}