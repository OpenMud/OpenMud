using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public readonly struct ProcArgument
{
    public readonly string? Name;
    public readonly EnvObjectReference Data;

    public ProcArgument(string? name, EnvObjectReference data)
    {
        Name = name;
        Data = data;
    }

    public ProcArgument(EnvObjectReference data) : this(null, data)
    {
    }
}