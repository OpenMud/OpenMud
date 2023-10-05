using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Utils;
//We don't want to embed this logic right onto atomic, since it will become accessible or collide with the naming of user-defined methods.

public struct VerbMetadata
{
    public readonly string procName;
    public readonly string verbName;
    public readonly string? category;
    public readonly string description;
    public readonly VerbSrc source;
    public readonly IReadOnlyList<ArgAsConstraint> argAsConstraints;
    public readonly IReadOnlyList<SimpleSourceConstraint> argSourceConstraints;
    public readonly IReadOnlyList<ListEvalSourceConstraint> listEvalSourceConstraint;

    public VerbMetadata(string procName, string verbName, string? category, string desc, VerbSrc source,
        IReadOnlyList<ArgAsConstraint> argAsConstraints, IReadOnlyList<SimpleSourceConstraint> argSourceConstraints,
        IReadOnlyList<ListEvalSourceConstraint> listEvalSourceConstraints)
    {
        this.procName = procName;
        this.verbName = verbName;
        this.category = category;
        this.source = source;
        description = desc;
        this.argAsConstraints = argAsConstraints;
        this.argSourceConstraints = argSourceConstraints;
        listEvalSourceConstraint = listEvalSourceConstraints;
    }
}

public static class VerbParsing
{
    public static IEnumerable<VerbMetadata> DiscoverVerbs(this Datum t)
    {
        if (t is Atom a)
        {
            var topLevelVerbs = a.Verbs;

            return topLevelVerbs;
        }

        return Enumerable.Empty<VerbMetadata>();
    }

    public static string ResolveVerb(this Datum t, string verb)
    {
        var procName = t.DiscoverVerbs().Where(x => x.verbName == verb).Take(1).ToList();

        if (!procName.Any())
            throw new Exception("Verb doesn't exist.");

        return procName.Single().procName;
    }
}