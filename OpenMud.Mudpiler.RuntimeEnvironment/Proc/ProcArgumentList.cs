using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public readonly struct ProcArgumentList
{
    private readonly ProcArgument[]? arguments;

    public int MaxPositionalArgument => arguments.Where(x => x.Name == null).Count();

    public ProcArgumentList()
    {
        arguments = new ProcArgument[0];
    }

    public ProcArgumentList(params ProcArgument[] arguments)
    {
        if (arguments == null)
            throw new Exception("Arguments cannot be null.");
        this.arguments = arguments ?? new ProcArgument[0];
    }

    public ProcArgumentList(params EnvObjectReference[] arguments)
    {
        this.arguments = arguments.Select(x => new ProcArgument(x)).ToArray();
    }

    public EnvObjectReference this[int idx] => Get(idx);

    public EnvObjectReference this[int idx, string name] => Get(idx, name);

    public EnvObjectReference this[string name] => Get(-1, name);

    public (ProcArgumentList first, ProcArgumentList last) Split(int splitIndex)
    {
        var firstCol = arguments.Take(splitIndex).ToArray();
        var lastCol = arguments.Skip(splitIndex).ToArray();

        return (new ProcArgumentList(firstCol), new ProcArgumentList(lastCol));
    }

    public EnvObjectReference Get(int idx = -1, string? argName = null, bool defaultNull = true)
    {
        if (arguments == null)
        {
            if (defaultNull)
                return VarEnvObjectReference.NULL;

            throw new IndexOutOfRangeException();
        }

        if (argName != null)
            foreach (var arg in arguments)
                if (arg.Name == argName)
                    return arg.Data;

        if (idx >= arguments.Length || idx < 0)
        {
            if (defaultNull)
                return VarEnvObjectReference.NULL;

            throw new IndexOutOfRangeException();
        }

        var w = 0;
        foreach (var a in arguments)
        {
            if (a.Name != null)
                continue;

            if (w == idx)
                return a.Data;

            w++;
        }

        if (defaultNull)
            return VarEnvObjectReference.NULL;

        throw new IndexOutOfRangeException();
    }

    public EnvObjectReference[] GetArgumentList(int argIdx = 0)
    {
        if (arguments == null)
            return new EnvObjectReference[0];

        return arguments.Where(x => x.Name == null).Skip(argIdx).Select(x => x.Data).ToArray();
    }

    public object[] MapArguments(string[] strings)
    {
        var thisExpr = this;

        return strings.Select((n, i) => (object)thisExpr.Get(i, n)).ToArray();
    }

    public WrappedProcArgumentList Wrap(Func<EnvObjectReference, object> wrapper)
    {
        if (arguments == null)
            return new WrappedProcArgumentList();

        return new WrappedProcArgumentList(
            arguments.Select(
                    a => new WrappedProcArgument(a.Name, wrapper(a.Data))
                )
                .ToArray()
        );
    }
}