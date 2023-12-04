namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings;

public enum SourceType
{
    User,
    UserContents,
    UserLoc,
    UserGroup,
    View,
    OView,
    Any,
    World,
    Clients,
    Range
}

public class VerbSrc : IDmlProcAttribute
{
    public static readonly VerbSrc Default = new(SourceType.User, 0);

    public VerbSrc(SourceType type, int argument = 0)
    {
        Source = type;
        Argument = argument;
    }

    public VerbSrc(SourceType type)
    {
        Source = type;

        if (Source == SourceType.View || Source == SourceType.OView)
            Argument = 5;
    }

    public SourceType Source { get; }
    public int Argument { get; }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Replace;
}