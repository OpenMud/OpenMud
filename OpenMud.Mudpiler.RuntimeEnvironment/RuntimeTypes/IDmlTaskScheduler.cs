using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public interface IDmlTaskScheduler
{
    void DeferExecution(DatumProcExecutionContext datumProcExecutionContext, int lengthMilliseconds);
    void ClearDeferExecution(DatumProcExecutionContext datumProcExecutionContext);
}