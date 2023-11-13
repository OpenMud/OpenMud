using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public delegate DatumProcExecutionContext BinaryHandler(EnvObjectReference subject, EnvObjectReference operand);

public delegate DatumProcExecutionContext TerneryHandler(EnvObjectReference subject, EnvObjectReference op0,
    EnvObjectReference op1);

public delegate DatumProcExecutionContext BinaryAssignmentHandler(EnvObjectReference subject,
    EnvObjectReference operand);

public delegate DatumProcExecutionContext UnaryHandler(EnvObjectReference subject);

public delegate DatumProcExecutionContext UnaryAssignmentHandler(EnvObjectReference subject);

public class OpObject : IOpSolver
{
    private readonly Dictionary<DmlBinaryAssignment, BinaryAssignmentHandler> binAsnHandlers = new();
    private readonly Dictionary<DmlBinary, BinaryHandler> binHandlers = new();

    private readonly IOpSolver primitive = new OpPrimitive();
    private readonly Dictionary<DmlTernery, TerneryHandler> ternHandlers = new();
    private readonly Dictionary<DmlUnaryAssignment, UnaryAssignmentHandler> unaryAsnHandlers = new();
    private readonly Dictionary<DmlUnary, UnaryHandler> unaryHandlers = new();

    public OpObject()
    {
        binHandlers[DmlBinary.Equivalent] = (a, b) => binHandlers[DmlBinary.Equals](a, b);
        binHandlers[DmlBinary.NotEquivalent] = (a, b) => new PreparedDatumProcContext(
            () => new VarEnvObjectReference(
                new PreparedChainDatumProcContext(
                    binHandlers[DmlBinary.Equivalent](a, b),
                    r => new PreparedDatumProcContext(() => !(dynamic)r)
                )
            )
        );

        binHandlers[DmlBinary.Equals] = (a, b) =>
            new PreparedDatumProcContext(() => new VarEnvObjectReference(a.Target.Equals(b.Target), true));
        binHandlers[DmlBinary.NotEqual] = (a, b) =>
            new PreparedDatumProcContext(() => new VarEnvObjectReference(!a.Target.Equals(b.Target), true));


        binAsnHandlers[DmlBinaryAssignment.Assignment] = DefaultUnaryAssignment;
        binAsnHandlers[DmlBinaryAssignment.CopyInto] = DefaultUnaryAssignment;

        unaryHandlers[DmlUnary.Logical] = a =>
            new PreparedDatumProcContext(() => new VarEnvObjectReference(a.Target != null, true));

        unaryHandlers[DmlUnary.Not] = a =>
            new PreparedDatumProcContext(() => new VarEnvObjectReference(a.Target == null, true));
    }

    public DatumProcExecutionContext BinaryAssignment(DmlBinaryAssignment op, EnvObjectReference subject,
        EnvObjectReference operand)
    {
        if (!binAsnHandlers.TryGetValue(op, out var handler))
            throw new DmlOperationNotImplemented();

        return handler(subject, operand);
    }

    public DatumProcExecutionContext Unary(DmlUnary op, EnvObjectReference subject)
    {
        if (!unaryHandlers.TryGetValue(op, out var handler))
            return primitive.Unary(op, subject);

        return handler(subject);
    }

    public DatumProcExecutionContext Binary(DmlBinary op, EnvObjectReference subject, EnvObjectReference operand)
    {
        if (!binHandlers.TryGetValue(op, out var handler))
            return primitive.Binary(op, subject, operand);

        return handler(subject, operand);
    }

    public DatumProcExecutionContext Ternery(DmlTernery op, EnvObjectReference subject, EnvObjectReference op0,
        EnvObjectReference op1)
    {
        if (!ternHandlers.TryGetValue(op, out var handler))
            return primitive.Ternery(op, subject, op0, op1);

        return handler(subject, op0, op1);
    }

    public DatumProcExecutionContext UnaryAssignment(DmlUnaryAssignment op, EnvObjectReference subject)
    {
        if (!unaryAsnHandlers.TryGetValue(op, out var handler))
            throw new DmlOperationNotImplemented();

        return handler(subject);
    }

    public void Override(DmlBinary op, BinaryHandler handler)
    {
        binHandlers[op] = handler;
    }

    public void Override(DmlUnary op, UnaryHandler handler)
    {
        unaryHandlers[op] = handler;
    }

    public void Override(DmlBinaryAssignment op, BinaryAssignmentHandler handler)
    {
        binAsnHandlers[op] = handler;
    }

    public void Override(DmlUnaryAssignment op, UnaryAssignmentHandler handler)
    {
        unaryAsnHandlers[op] = handler;
    }

    public void Override(DmlTernery op, TerneryHandler handler)
    {
        ternHandlers[op] = handler;
    }

    public DatumProcExecutionContext DefaultUnaryAssignment(EnvObjectReference subject, EnvObjectReference src)
    {
        return new PreparedDatumProcContext(() =>
        {
            subject.Assign(src);
            return new VarEnvObjectReference(subject, true);
        });
    }

    public DatumProcExecutionContext PrimitiveCast(EnvObjectReference subject, EnvObjectReference type)
    {
        throw new DmlOperationNotImplemented();
    }
}