namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings;

public class VerbDescription : IDmlProcAttribute
{
    public VerbDescription(string desc)
    {
        Desc = desc;
    }

    public string Desc { get; }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
}