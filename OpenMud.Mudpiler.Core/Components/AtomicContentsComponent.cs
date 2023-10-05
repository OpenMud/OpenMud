namespace OpenMud.Mudpiler.Core.Components;

public struct AtomicContentsComponent
{
    public readonly string[] Contents;

    public AtomicContentsComponent(string[] contents)
    {
        Contents = contents;
    }
}