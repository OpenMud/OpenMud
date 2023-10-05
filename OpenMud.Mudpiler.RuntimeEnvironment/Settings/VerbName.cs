namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings;

public class VerbName : IDmlProcAttribute
{
    public VerbName(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
}