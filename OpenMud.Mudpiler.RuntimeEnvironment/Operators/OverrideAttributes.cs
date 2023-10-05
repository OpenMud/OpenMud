namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public class BinOpOverride : IDmlProcAttribute
{
    public readonly DmlBinary Operation;

    public BinOpOverride(DmlBinary operation)
    {
        Operation = operation;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class BinOpAsnOverride : IDmlProcAttribute
{
    public readonly DmlBinaryAssignment Operation;

    public BinOpAsnOverride(DmlBinaryAssignment operation)
    {
        Operation = operation;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class UnOpOverride : IDmlProcAttribute
{
    public readonly DmlUnary Operation;

    public UnOpOverride(DmlUnary operation)
    {
        Operation = operation;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class UnOpAsnpOverride : IDmlProcAttribute
{
    public readonly DmlUnaryAssignment[] Operations;

    public UnOpAsnpOverride(params DmlUnaryAssignment[] operations)
    {
        Operations = operations;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}

public class TernOpAsnpOverride : IDmlProcAttribute
{
    public readonly DmlTernery Operation;

    public TernOpAsnpOverride(DmlTernery operation)
    {
        Operation = operation;
    }

    public ProcAttributeCoalesceStrategy CoalesceStrategy => ProcAttributeCoalesceStrategy.Concat;
}