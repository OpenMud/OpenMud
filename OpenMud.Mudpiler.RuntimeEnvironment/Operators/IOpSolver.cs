using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public interface IOpSolver
{
    DatumProcExecutionContext Binary(DmlBinary op, EnvObjectReference subject, EnvObjectReference operand);
    DatumProcExecutionContext Unary(DmlUnary op, EnvObjectReference subject);

    DatumProcExecutionContext BinaryAssignment(DmlBinaryAssignment op, EnvObjectReference subject,
        EnvObjectReference operand);

    DatumProcExecutionContext UnaryAssignment(DmlUnaryAssignment op, EnvObjectReference subject);

    DatumProcExecutionContext Ternery(DmlTernery op, EnvObjectReference subject, EnvObjectReference op0,
        EnvObjectReference op1);

    DatumProcExecutionContext PrimitiveCast(EnvObjectReference subject, EnvObjectReference type);
}