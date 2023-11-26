namespace OpenMud.Mudpiler.RuntimeEnvironment.Settings;

public enum ArgAs
{
    Mob,
    Obj,
    Anything,
    Turf,
    Num,
    Text,
    Password,
    Message,
    CommandText,
    Icon,
    Area,
    Null,
    Key,
    Color,
    File,
    Sound
}

public class ListEvalSourceConstraint : IDmlProcAttribute
{
    public readonly int ArgIndex;
    public readonly string[] generators;

    public ListEvalSourceConstraint(int argIdx, params string[] generators)
    {
        ArgIndex = argIdx;
        this.generators = generators;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class VariableEvalSourceConstraint : IDmlProcAttribute
{
    public readonly string Name;

    public VariableEvalSourceConstraint(string name)
    {
        Name = name;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class ArgAsConstraint : IDmlProcAttribute
{
    public readonly int ArgIndex;
    public readonly ArgAs Expected;
    public readonly int Rank;

    public ArgAsConstraint(int argIdx, int rank, ArgAs expected)
    {
        ArgIndex = argIdx;
        Expected = expected;
        Rank = rank;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class SimpleSourceConstraint : IDmlProcAttribute
{
    public readonly int ArgIndex;
    public readonly SourceType Src;
    public readonly int SrcOperand;

    public SimpleSourceConstraint(int argIdx, SourceType src, int srcOperand)
    {
        ArgIndex = argIdx;
        Src = src;
        SrcOperand = srcOperand;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}