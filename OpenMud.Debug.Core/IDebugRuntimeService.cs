namespace OpenMud.Debug.Core;
public struct ExecutionPoint
{
    public readonly string File;
    public readonly int Line;
}

public delegate void DebugRuntimeRequest(IDebugContext ctx);

public interface IDebugRuntimeService
{
    ExecutionPoint Current { get; }
    bool Running { get; }
    string[] LocalVariables();

    public void Request(DebugRuntimeRequest request);
}