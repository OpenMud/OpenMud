namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings;

public class VerbCategory : IDmlProcAttribute
{
    public VerbCategory(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
}