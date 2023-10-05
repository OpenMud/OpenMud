namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public delegate void DestroyObjectCallback(object o);

public interface IWeakDestroyRegister
{
    public void Subscribe(DestroyObjectCallback OnDestroy);
    public void Unsubscribe(DestroyObjectCallback OnDestroy);
}

public class WeakDestroyCallback : IWeakDestroyRegister
{
    private object _internal;
    private readonly HashSet<WeakReference<DestroyObjectCallback>> subscribes = new();

    public WeakDestroyCallback(object _internal)
    {
        this._internal = _internal;
    }

    public void Subscribe(DestroyObjectCallback observer)
    {
        subscribes.RemoveWhere(x => !x.TryGetTarget(out var _));
        subscribes.Add(new WeakReference<DestroyObjectCallback>(observer));
    }

    public void Unsubscribe(DestroyObjectCallback observer)
    {
        subscribes.Remove(new WeakReference<DestroyObjectCallback>(observer));
    }

    public void Destroy()
    {
        foreach (var s in subscribes)
            if (s.TryGetTarget(out var t))
                t(_internal);

        _internal = null;
    }
}