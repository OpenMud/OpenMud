using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public class TimeTaskScheduler : IDmlTaskScheduler
{
    private readonly Dictionary<DatumProcExecutionContext, float> deferred = new();

    public void ClearDeferExecution(DatumProcExecutionContext datumProcExecutionContext)
    {
        deferred.Remove(datumProcExecutionContext);
    }

    public void DeferExecution(DatumProcExecutionContext datumProcExecutionContext, int delayMilliseconds)
    {
        deferred[datumProcExecutionContext] = delayMilliseconds / 1000.0f;
    }

    public void Update(float deltaTimeSeconds)
    {
        var execute = new List<DatumProcExecutionContext>();
        foreach (var (k, v) in deferred.ToList())
        {
            deferred[k] = v - deltaTimeSeconds;

            if (deferred[k] <= 0)
            {
                execute.Add(k);
                deferred.Remove(k);
            }
        }

        foreach (var e in execute)
            e.Continue();
    }
}