using System.Collections.ObjectModel;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Framework.Datums;

public sealed class DatumDmlList : DmlList
{
    private readonly ObservableCollection<DmlListItem> host = new();

    public DatumDmlList()
    {
        host.CollectionChanged += (_, _) => Changed?.Invoke();
    }

    public override IList<DmlListItem> Host => host;
    public override Action? Changed { get; set; }
}