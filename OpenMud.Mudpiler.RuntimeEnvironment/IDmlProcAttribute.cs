namespace OpenMud.Mudpiler.RuntimeEnvironment;

public enum ProcAttributeCoalesceStrategy
{
    Replace,
    Concat
}

public interface IDmlProcAttribute
{
    ProcAttributeCoalesceStrategy CoalesceStrategy { get; }
}