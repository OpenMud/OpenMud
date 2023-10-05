namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public sealed class VarEnvObjectReference : EnvObjectReference
{
    public static readonly EnvObjectReference NULL = new VarEnvObjectReference(null, true);

    private object _internal;

    private DestroyObjectCallback destoryObjectListener;

    private bool immutable;

    public VarEnvObjectReference() : this(null)
    {
    }

    public VarEnvObjectReference(object _internal, bool immutable = false, IWeakDestroyRegister destoryNotify = null)
    {
        AssignRaw(_internal);
        this.immutable = immutable;

        if (destoryNotify != null)
        {
            if (destoryNotifySource != null)
                throw new Exception("Conflicting destroy notify sources.");

            destoryObjectListener = OnDestroy;
            destoryNotifySource = destoryNotify;
            destoryNotify.Subscribe(destoryObjectListener);
        }
    }

    public override object Target => _internal;

    public override bool IsAssignable => !immutable;

    public VarEnvObjectReference Clone()
    {
        return new VarEnvObjectReference(this, immutable, destoryNotifySource);
    }

    protected override void OnDestroy(object value)
    {
        if (value == ExtractInternal(_internal).src)
            _internal = null;
    }

    protected override void AssignRaw(object o, bool makeImmutable = false)
    {
        if (immutable)
            throw new Exception("Reference is not mutable. Cannot assign.");

        if (destoryNotifySource != null)
            destoryNotifySource.Unsubscribe(OnDestroy);

        (destoryNotifySource, _internal) = ExtractInternal(o);

        if (destoryNotifySource != null)
        {
            destoryObjectListener = OnDestroy;
            destoryNotifySource.Subscribe(destoryObjectListener);
        }

        if (makeImmutable)
            immutable = true;
    }

    public static EnvObjectReference CreateImmutable(object _internal)
    {
        return new VarEnvObjectReference(_internal, true);
    }

    public static EnvObjectReference Variable(EnvObjectReference asn)
    {
        return new VarEnvObjectReference(asn);
    }

    public static EnvObjectReference Variable()
    {
        return new VarEnvObjectReference(null);
    }

    public static dynamic Wrap(dynamic _internal)
    {
        return new VarEnvObjectReference(_internal);
    }
}