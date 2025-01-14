using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public delegate void ProcStep(IDebuggableProc proc, int lineNumber);
public delegate void ProcFault(IDebuggableProc proc, Exception ex);

public interface IDebuggableProc
{
    public static abstract event ProcStep Step;
    public static abstract event ProcFault Fault;
    
    public static abstract string SourceFileName { get; }
    public static abstract int[] ExecutedSourceLines { get; }
    
    public Dictionary<string, EnvObjectReference> ImmutableLocalScope { get; }
}
