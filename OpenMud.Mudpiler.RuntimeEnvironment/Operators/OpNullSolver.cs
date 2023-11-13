using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public class OpNullSolver : IOpSolver
{
    private readonly OpPrimitive primitiveSolver = new();

    public DatumProcExecutionContext Binary(DmlBinary op, EnvObjectReference subject, EnvObjectReference operand)
    {
        return new PreparedDatumProcContext(() =>
        {
            dynamic ret;
            switch (op)
            {
                case DmlBinary.LogicalOr:
                    ret = primitiveSolver.ImmediateUnary(DmlUnary.Logical, operand);
                    break;
                case DmlBinary.LogicalAnd:
                    ret = false;
                    break;
                case DmlBinary.Addition:
                    ret = operand;
                    break;
                default:
                    ret = primitiveSolver.ImmediateBinary(op, subject, operand);
                    break;
            }

            return new VarEnvObjectReference(ret, true);
        });
    }

    public DatumProcExecutionContext BinaryAssignment(DmlBinaryAssignment op, EnvObjectReference subject,
        EnvObjectReference operand)
    {
        return primitiveSolver.BinaryAssignment(op, subject, operand);
    }

    public DatumProcExecutionContext Ternery(DmlTernery op, EnvObjectReference subject, EnvObjectReference op0,
        EnvObjectReference op1)
    {
        throw new DmlOperationNotImplemented();
    }

    public DatumProcExecutionContext Unary(DmlUnary op, EnvObjectReference subject)
    {
        return new PreparedDatumProcContext(() =>
        {
            dynamic ret;
            if (op == DmlUnary.Logical)
                ret = false;
            else if (op == DmlUnary.Not)
                ret = true;
            else
                ret = primitiveSolver.ImmediateUnary(op, new VarEnvObjectReference(0, true));

            return new VarEnvObjectReference(ret, true);
        });
    }

    public DatumProcExecutionContext UnaryAssignment(DmlUnaryAssignment op, EnvObjectReference subject)
    {
        return primitiveSolver.UnaryAssignment(op, new VarEnvObjectReference(0, true));
    }

    public DatumProcExecutionContext PrimitiveCast(EnvObjectReference subject, EnvObjectReference type)
    {
        throw new DmlOperationNotImplemented();
    }
}