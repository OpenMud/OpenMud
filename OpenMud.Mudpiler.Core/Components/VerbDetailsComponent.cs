using System.Collections.Immutable;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;

namespace OpenMud.Mudpiler.Core.Components;

internal struct VerbDetails
{
    public readonly string MethodName;
    public readonly string Category;
    public readonly VerbSrc? SrcConstraints;
    public readonly IImmutableList<ArgAsConstraint> ArgAsConstraints;
    public readonly IImmutableList<SimpleSourceConstraint> ArgSourceConstraints;
    public readonly IImmutableList<ListEvalSourceConstraint> ListEvalArgSourceConstraints;

    public VerbDetails(string methodName, string category, VerbSrc? verbSrc,
        IEnumerable<ArgAsConstraint> argAsConstraints, IEnumerable<SimpleSourceConstraint> argSourceConstraints,
        IEnumerable<ListEvalSourceConstraint> listEvalConstraint)
    {
        MethodName = methodName;
        Category = category;
        SrcConstraints = verbSrc;
        ArgAsConstraints = argAsConstraints.ToImmutableList();
        ArgSourceConstraints = argSourceConstraints.ToImmutableList();
        ListEvalArgSourceConstraints = listEvalConstraint.ToImmutableList();
    }
}

internal struct VerbDetailsComponent
{
    public readonly Dictionary<string, VerbDetails> Verbs;

    public VerbDetailsComponent()
    {
        Verbs = new Dictionary<string, VerbDetails>();
    }
}