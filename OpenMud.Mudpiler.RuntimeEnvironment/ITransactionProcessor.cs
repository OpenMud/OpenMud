using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public delegate DatumProcExecutionContext ExecuteTransaction(DatumProcExecutionContext? caller,
    EnvObjectReference source, EnvObjectReference destination, ProcArgumentList args, string name,
    long? precedence = null);

public delegate DatumProcExecutionContext ExecuteWithContext(DatumProcExecutionContext? caller, EntityHandle source,
    EntityHandle target, string verb, ProcArgumentList arguments);

public delegate IEnumerable<VerbMetadata> DiscoverVerbs(EntityHandle target);

public interface ITransactionProcessor
{
    bool ProcExists(string name);
    DatumProcExecutionContext Invoke(DatumProcExecutionContext? caller, string name, ProcArgumentList args);

    DatumProcExecutionContext InvokeOn(DatumProcExecutionContext? caller, EnvObjectReference subject, string name,
        ProcArgumentList args);

    DatumProcExecutionContext InvokePrec(DatumProcExecutionContext? caller, string name, long prec,
        ProcArgumentList args);
}