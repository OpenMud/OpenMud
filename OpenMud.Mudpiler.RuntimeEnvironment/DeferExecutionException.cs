namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class DeferExecutionException : Exception
{
    public readonly int LengthMilliseconds;

    public DeferExecutionException(int lengthMs)
    {
        LengthMilliseconds = lengthMs;
    }
}