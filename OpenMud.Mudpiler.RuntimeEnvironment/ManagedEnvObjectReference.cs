using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class ManagedEnvObjectReference : EnvObjectReference
{
    private Action<object>? assignHandler;
    private Func<object>? getterHandler;
    private bool isImmutable;

    public ManagedEnvObjectReference()
    {
    }

    public ManagedEnvObjectReference(Func<object> getterHandler, Action<object>? assignHandler = null)
    {
        this.assignHandler = assignHandler;
        this.getterHandler = getterHandler;
    }

    public override bool IsAssignable => assignHandler != null;

    public override object Target
    {
        get
        {
            if (getterHandler != null)
                return getterHandler();

            throw new Exception("Not bound.");
        }
    }

    public void Bind(ManagedEnvObjectReference src)
    {
        if (getterHandler != null)
            throw new Exception("Already bound.");

        assignHandler = src.assignHandler;
        getterHandler = src.getterHandler;
    }

    protected override void AssignRaw(object o, bool makeImmutable = false)
    {
        if (assignHandler == null)
            throw new Exception("Setter is not bound.");

        if (isImmutable)
            throw new Exception("Not mutable.");

        (destoryNotifySource, var _internal) = ExtractInternal(o);
        assignHandler(_internal);

        isImmutable |= makeImmutable;
    }

    protected override void OnDestroy(object value)
    {
    }
}