namespace OpenMud.Debug.Core;

public delegate void StopEvent();

//This is the interface that the debugBridge core server uses to communicate with the runtime.
public interface IDebugClient
{
    public event StopEvent OnStop;
    
    void SetBreakpoint(ExecutionPoint breakpoint);
    void ClearBreakpoint(ExecutionPoint breakpoint);

    void Continue();

    void Shutdown();
    
    void Start();

    void PollEvents();
}
