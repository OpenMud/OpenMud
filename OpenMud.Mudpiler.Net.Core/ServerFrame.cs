namespace OpenMud.Mudpiler.Net.Core;

public struct ServerFrame
{
    public readonly long NetworkFrame;
    public readonly float DeltaSeconds;

    public ServerFrame(long networkFrame, float deltaSeconds)
    {
        NetworkFrame = networkFrame;
        DeltaSeconds = deltaSeconds;
    }
}