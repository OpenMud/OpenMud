using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class EntityHandle : DatumHandle
{
    private readonly DiscoverVerbs discover;
    private readonly Func<bool> isInWorld;
    private readonly ExecuteWithContext verbHandler;

    public EntityHandle(MudEnvironment environment, IDmlTaskScheduler scheduler, Func<bool> isInWorld,
        Func<DatumHandle, EnvObjectReference> lookup, Func<EnvObjectReference, object> wrap,
        Func<object, EnvObjectReference> unwrap, ExecuteWithContext verbHandler, DiscoverVerbs discover,
        ITransactionProcessor execHandler, DestoryCallback destroyAllReferences)
        : base(environment, scheduler, lookup, wrap, unwrap, execHandler, destroyAllReferences)
    {
        this.isInWorld = isInWorld;
        this.lookup = lookup;
        this.verbHandler = verbHandler;
        this.discover = discover;
    }

    public bool IsInWorld => isInWorld();

    public IEnumerable<string> DiscoverVerbs(EntityHandle subject, string category = null)
    {
        return discover(subject).Where(x => category == null || x.category == category).Select(x => x.verbName);
    }

    public IEnumerable<string> DiscoverVerbCategories(EntityHandle subject, string category = null)
    {
        return discover(subject).Select(x => x.category).Where(x => x != null).Distinct();
    }

    public IEnumerable<VerbMetadata> DiscoverVerbMetadata(EntityHandle subject, string category = null)
    {
        return discover(subject).Where(x => category == null || x.category == category).ToList();
    }

    public VerbSrc DiscoverOwnedVerbSource(string verb)
    {
        return discover(this).Where(x => x.verbName == verb).Select(x => x.source).Single();
    }

    public WrappedDatumProcContext Interact(EntityHandle subject, string verb, params object[] arguments)
    {
        var ex = verbHandler(null, this, subject, verb,
            new ProcArgumentList(arguments.Select(VarEnvObjectReference.CreateImmutable).ToArray()));

        ex.Continue();

        return new WrappedDatumProcContext(ex, wrap);
    }
}