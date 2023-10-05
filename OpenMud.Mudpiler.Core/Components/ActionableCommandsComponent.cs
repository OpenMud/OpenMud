namespace OpenMud.Mudpiler.Core.Components;

public struct CommandDetails
{
    public readonly int Precedent;
    public readonly string? Target;
    public readonly string? TargetName;
    public readonly string Verb;

    public bool IsNull => Verb == null;

    public CommandDetails(int precedent, string verb, string? targetName = null, string? target = null)
    {
        Precedent = precedent;
        Verb = verb;
        Target = target;
        TargetName = targetName;
    }

    public override bool Equals(object? obj)
    {
        return obj is CommandDetails details &&
               Precedent == details.Precedent &&
               Target == details.Target &&
               TargetName == details.TargetName &&
               Verb == details.Verb &&
               IsNull == details.IsNull;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Precedent, Target, TargetName, Verb, IsNull);
    }
}

public struct ActionableCommandsComponent
{
    public readonly CommandDetails[] Commands;

    public ActionableCommandsComponent(CommandDetails[] commandDetails) : this()
    {
        Commands = commandDetails;
    }

    public override bool Equals(object? obj)
    {
        return obj is ActionableCommandsComponent component &&
               Commands.Length == component.Commands.Length && Commands.Except(component.Commands).Count() == 0;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Commands);
    }
}