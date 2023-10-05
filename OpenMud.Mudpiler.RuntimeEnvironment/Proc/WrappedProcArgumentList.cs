using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public class WrappedProcArgument
{
    public readonly object Data;
    public readonly string? Name;

    public WrappedProcArgument(string? name, object data)
    {
        Name = name;
        Data = data;
    }

    public WrappedProcArgument(object data) : this(null, data)
    {
    }
}

public class WrappedProcArgumentList
{
    private readonly WrappedProcArgument[]? arguments;

    public WrappedProcArgumentList()
    {
        arguments = new WrappedProcArgument[0];
    }

    public WrappedProcArgumentList(params WrappedProcArgument[] arguments)
    {
        if (arguments == null)
            throw new Exception("Arguments cannot be null.");
        this.arguments = arguments ?? new WrappedProcArgument[0];
    }

    public WrappedProcArgumentList(params object[] arguments)
    {
        this.arguments = arguments.Select(x => new WrappedProcArgument(x)).ToArray();
    }

    public int MaxPositionalArgument => arguments.Where(x => x.Name == null).Count();

    public ProcArgumentList Unwrap(Func<object, EnvObjectReference> unwrapper)
    {
        return new ProcArgumentList(
            arguments.Select(
                    a => new ProcArgument(a.Name, unwrapper(a.Data))
                )
                .ToArray()
        );
    }
}