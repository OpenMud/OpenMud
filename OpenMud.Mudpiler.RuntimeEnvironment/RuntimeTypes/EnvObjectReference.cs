using System.Collections;
using System.Dynamic;
using System.Reflection;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public delegate void DestoryCallback();

public abstract class EnvObjectReference : DynamicObject, IEnumerable<EnvObjectReference>
{
    protected IWeakDestroyRegister destoryNotifySource;

    private readonly DestroyObjectCallback destroyListener;

    public EnvObjectReference(IWeakDestroyRegister destoryNotify = null)
    {
        if (destoryNotify != null)
        {
            if (destoryNotifySource != null)
                throw new Exception("Conflicting destroy notify sources.");

            destoryNotifySource = destoryNotify;
            destroyListener = OnDestroy;
            destoryNotify.Subscribe(destroyListener);
        }
    }

    public abstract object Target { get; }

    public Type Type => Target == null ? null : Target.GetType();

    public bool IsNull => Target == null;

    public abstract bool IsAssignable { get; }

    public IEnumerator<EnvObjectReference> GetEnumerator()
    {
        if (Target is IEnumerable<EnvObjectReference> en)
            return en.GetEnumerator();

        throw new Exception("Not an enumerable type.");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        if (Target is IEnumerable en)
            return en.GetEnumerator();

        throw new Exception("Not an enumerable type.");
    }

    public event Action? Assigned;

    protected abstract void OnDestroy(object value);

    public T GetOrDefault<T>(T defaultValue)
    {
        return IsNull ? defaultValue : (T)Target;
    }

    protected static (IWeakDestroyRegister destroyNotify, object src) ExtractInternal(object v)
    {
        IWeakDestroyRegister cbk = null;
        while (v is EnvObjectReference r)
            (cbk, v) = (r.destoryNotifySource, r.Target);

        return (cbk, v);
    }

    protected abstract void AssignRaw(object o, bool makeImmutable = false);

    public void Assign(EnvObjectReference _internal, bool makeImmutable = false)
    {
        AssignRaw(_internal, makeImmutable);
        Assigned?.Invoke();
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        result = null;

        var m = Type.GetMethod(binder.Name);

        if (m == null)
            return false;

        result = Invoke(m, args);

        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        var fld = Type.GetField(binder.Name);

        if (fld != null)
        {
            fld.SetValue(fld.IsStatic ? null : Target, value);
            return true;
        }

        var prop = Type.GetProperty(binder.Name);
        if (prop != null)
        {
            prop.SetValue(Target, value);
            return true;
        }

        return false;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        result = null;

        var fld = Type.GetField(binder.Name);
        var found = false;

        if (fld != null)
        {
            result = fld.GetValue(fld.IsStatic ? null : Target);
            found = true;
        }

        var prop = Type.GetProperty(binder.Name);
        if (prop != null)
        {
            result = prop.GetValue(Target);
            found = true;
        }

        if (!(result is EnvObjectReference))
            result = VarEnvObjectReference.CreateImmutable(result);

        return found;
    }

    public T Get<T>()
    {
        if (IsNull)
            throw new Exception("Value is not defined.");

        return (T)Target;
    }

    public bool TryGet<T>(out T result)
    {
        result = default;

        if (IsNull)
            return false;

        if (!typeof(T).IsAssignableFrom(Type))
            return false;

        result = Get<T>();

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EnvObjectReference reference &&
               EqualityComparer<object>.Default.Equals(Target, reference.Target);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Target);
    }


    // Implicit conversion to int
    public static implicit operator int(EnvObjectReference wrapper)
    {
        return (int)wrapper.Target;
    }

    // Implicit conversion to float
    public static implicit operator float(EnvObjectReference wrapper)
    {
        return (float)wrapper.Target;
    }

    // Implicit conversion to double
    public static implicit operator double(EnvObjectReference wrapper)
    {
        return (double)wrapper.Target;
    }

    // Implicit conversion to string
    public static implicit operator string(EnvObjectReference wrapper)
    {
        return (string)wrapper.Target;
    }

    // Implicit conversion to bool
    public static implicit operator bool(EnvObjectReference wrapper)
    {
        return (bool)wrapper.Target;
    }


    // Implicit conversion to int
    public static implicit operator EnvObjectReference(int wrapper)
    {
        return VarEnvObjectReference.CreateImmutable(wrapper);
    }

    // Implicit conversion to float
    public static implicit operator EnvObjectReference(float wrapper)
    {
        return VarEnvObjectReference.CreateImmutable(wrapper);
    }

    // Implicit conversion to double
    public static implicit operator EnvObjectReference(double wrapper)
    {
        return VarEnvObjectReference.CreateImmutable(wrapper);
    }

    // Implicit conversion to bool
    public static implicit operator EnvObjectReference(bool wrapper)
    {
        return VarEnvObjectReference.CreateImmutable(wrapper);
    }

    // Implicit conversion to string
    public static implicit operator EnvObjectReference(string wrapper)
    {
        return VarEnvObjectReference.CreateImmutable(wrapper);
    }

    internal object Invoke(string name, ProcArgumentList arguments)
    {
        var mt = Type.GetMethod(name);

        if (mt != null)
            return Invoke(mt, arguments.GetArgumentList());
        if (Target is Datum d && d.HasProc(name))
            return d.Invoke(null, name, null, arguments);
        throw new Exception("Method does not exist.");
    }


    private static object[] FitExecutionArguments(Type[] argumentTypes, bool trailingParams, object[] arguments)
    {
        var argIsDynamic = argumentTypes.Select(n => IsTypeDynamic(n, null));
        var concreteParams = argumentTypes.Length;
        if (trailingParams)
            concreteParams -= 1;

        var fitArguments = arguments.Take(concreteParams).ToList();

        List<object> naturalArgs = fitArguments
            .Concat(Enumerable.Range(0, concreteParams - fitArguments.Count).Select(_ => (object)null)).ToList();

        var effectiveArgs = naturalArgs.Zip(argIsDynamic)
            .Select(v => v.Second ? VarEnvObjectReference.CreateImmutable(v.First) : v.First);

        if (trailingParams)
        {
            var trailing = arguments.Skip(concreteParams).ToArray();

            if (trailing.Length == 1 && trailing[0].GetType().IsArray)
                trailing = (object[])trailing.Single();

            if (argIsDynamic.Last())
                effectiveArgs = effectiveArgs.Append(trailing.Select(VarEnvObjectReference.CreateImmutable).ToArray());
            else
                effectiveArgs = effectiveArgs.Append(trailing.ToArray());
        }

        return effectiveArgs.ToArray();
    }

    private object Invoke(MethodInfo name, object[] arguments)
    {
        var argTypeList = name.GetParameters().Select(n => n.ParameterType).ToArray();
        var trailingArgList = name.GetParameters().Length > 0 &&
                              name.GetParameters().Last().GetCustomAttribute<ParamArrayAttribute>() != null;
        return name.Invoke(Target, FitExecutionArguments(argTypeList, trailingArgList, arguments));
    }

    internal object InvokeIfPresent(string name, object[] arguments)
    {
        var mt = Type.GetMethod(name);

        if (mt != null)
            return Invoke(mt, arguments);
        if (Target is Datum d && d.HasProc(name))
            return Invoke(name,
                new ProcArgumentList(arguments.Select(VarEnvObjectReference.CreateImmutable).ToArray()));

        return null;
    }

    private void SetFieldRaw(string name, object value)
    {
        var fld = Type.GetField(name);

        if (fld != null)
        {
            fld.SetValue(Target, value);
            return;
        }

        var prop = Type.GetProperty(name);
        if (prop != null)
        {
            prop.SetValue(Target, value);
            return;
        }

        throw new Exception("Field not found.");
    }

    internal void SetField(string name, object value)
    {
        if (IsFieldDynamic(name, GetField<object>(name)))
        {
            var src = new VarEnvObjectReference(value);
            var dyn = GetField<EnvObjectReference>(name);
            if (dyn == null)
                SetFieldRaw(name, src);
            else
                dyn.Assign(src);
        }
        else
        {
            SetFieldRaw(name, ExtractInternal(value));
        }
    }

    internal T GetField<T>(string name)
    {
        var fld = Type.GetField(name);

        if (fld != null)
            return (T)fld.GetValue(Target);

        var prop = Type.GetProperty(name);
        if (prop != null)
            return (T)prop.GetValue(Target);

        throw new Exception("Field not found.");
    }

    private static bool IsTypeDynamic(Type t, object? data)
    {
        if (t.IsEquivalentTo(typeof(object)) && data != null)
            t = data.GetType();

        //If data is null or an object, and the type is an object, we assume it is dynamic
        if (t.IsEquivalentTo(typeof(object)))
            return true;

        if (t.IsArray)
            t = t.GetElementType();

        if (typeof(EnvObjectReference).IsAssignableFrom(t))
            return true;

        return false;
    }

    internal bool IsFieldDynamic(string name, object data)
    {
        var fld = Type.GetField(name);

        if (fld != null)
            return IsTypeDynamic(fld.FieldType, data);

        var prop = Type.GetProperty(name);
        if (prop != null)
            return IsTypeDynamic(prop.PropertyType, data);

        throw new Exception("Field not found.");
    }

    public T TryGetOrDefault<T>(T v)
    {
        if (TryGet<T>(out var r))
            return r;

        return v;
    }
}