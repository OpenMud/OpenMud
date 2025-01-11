namespace OpenMud.Debug.Core;

public interface IDebugContext
{
    void SetBreakpoint(ExecutionPoint breakpoint);
    void ClearBreakpoint(ExecutionPoint breakpoint);

    void Continue();

    void Shutdown();
    void Start();
}