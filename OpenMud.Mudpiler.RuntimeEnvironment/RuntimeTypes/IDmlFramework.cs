using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public struct DmlRuntimeTypeDescriptor
{
    public readonly int Precedence;
    public readonly string TypePath;
    public readonly Type Type;

    public DmlRuntimeTypeDescriptor(int precedence, string typePath, Type type)
    {
        Precedence = precedence;
        TypePath = typePath;
        Type = type;
    }

    public DmlRuntimeTypeDescriptor(string typePath, Type type) :
        this(0, typePath, type)
    {
    }
}

public interface IDmlFramework
{
    DmlRuntimeTypeDescriptor[] Types { get; }
    IDmlTaskScheduler Scheduler { get; }
    IRuntimeTypeBuilder[] CreateBuilders(ITypeSolver typeSovler, ObjectInstantiator instantiator);
}

public class NullDmlFramework : IDmlFramework
{
    public DmlRuntimeTypeDescriptor[] Types { get; } = Array.Empty<DmlRuntimeTypeDescriptor>();
    public IDmlTaskScheduler Scheduler => new NullScheduler();

    public IRuntimeTypeBuilder[] CreateBuilders(ITypeSolver typeSovler, ObjectInstantiator instantiator)
    {
        return Array.Empty<IRuntimeTypeBuilder>();
    }
}

public class NullScheduler : IDmlTaskScheduler
{
    public void ClearDeferExecution(DatumProcExecutionContext datumProcExecutionContext)
    {
    }

    public void DeferExecution(DatumProcExecutionContext datumProcExecutionContext, int delaySleepTime)
    {
        throw new NotImplementedException();
    }
}