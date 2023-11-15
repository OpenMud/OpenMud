using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public sealed class OpNative : IOpSolver
{
    public DatumProcExecutionContext Binary(DmlBinary op, EnvObjectReference subjectRef, EnvObjectReference operandRef)
    {
        return new PreparedDatumProcContext(() => ImmediateBinary(op, subjectRef, operandRef));
    }

    public DatumProcExecutionContext BinaryAssignment(DmlBinaryAssignment op, EnvObjectReference subject,
        EnvObjectReference operand)
    {
        return new PreparedDatumProcContext(() =>
        {
            object buffer;
            switch (op)
            {
                case DmlBinaryAssignment.Assignment:
                    buffer = operand;
                    subject.Assign(operand);
                    break;
                case DmlBinaryAssignment.CopyInto:
                    buffer = operand;
                    subject.Assign(operand);
                    break;
                default:
                    throw new DmlOperationNotImplemented();
            }

            var result = buffer;

            if (subject.IsAssignable)
                subject.Assign(new VarEnvObjectReference(result));

            return new VarEnvObjectReference(result, true);
        });
    }

    public DatumProcExecutionContext Unary(DmlUnary op, EnvObjectReference subjectRef)
    {
        return new PreparedDatumProcContext(() => ImmediateUnary(op, subjectRef));
    }

    public DatumProcExecutionContext UnaryAssignment(DmlUnaryAssignment op, EnvObjectReference subjectRef)
    {
        return new PreparedDatumProcContext(() => { throw new DmlOperationNotImplemented(); });
    }

    public DatumProcExecutionContext Ternery(DmlTernery op, EnvObjectReference subject, EnvObjectReference op0,
        EnvObjectReference op1)
    {
        throw new DmlOperationNotImplemented();
    }

    public EnvObjectReference ImmediateBinary(DmlBinary op, EnvObjectReference subjectRef,
        EnvObjectReference operandRef)
    {
        dynamic subject = subjectRef.Target;
        dynamic operand = operandRef.Target;
        dynamic res;

        switch (op)
        {
            case DmlBinary.LogicalAnd:
                res = ImmediateUnary(DmlUnary.Logical, subjectRef).Get<bool>() &&
                      ImmediateUnary(DmlUnary.Logical, operandRef).Get<bool>();
                break;
            case DmlBinary.LogicalOr:
                res = ImmediateUnary(DmlUnary.Logical, subjectRef).Get<bool>() ||
                      ImmediateUnary(DmlUnary.Logical, operandRef).Get<bool>();
                break;
            case DmlBinary.Equals:
            case DmlBinary.Equivalent:
                if (subject == null && subject != operand)
                    res = false;
                else
                    res = subject.Equals(operand);
                break;
            case DmlBinary.NotEquivalent:
            case DmlBinary.NotEqual:
                res = !ImmediateBinary(DmlBinary.Equals, subjectRef, operandRef).Get<bool>();
                break;
            default:
                throw new DmlOperationNotImplemented();
        }

        return new VarEnvObjectReference(res, true);
    }

    public EnvObjectReference ImmediateUnary(DmlUnary op, EnvObjectReference subjectRef)
    {
        dynamic subject = subjectRef.Target;
        dynamic res;
        switch (op)
        {
            case DmlUnary.Logical:
                res = DmlEnv.AsLogical(subject);
                break;
            case DmlUnary.AsString:
                res = subject.ToString();
                break;
            case DmlUnary.Not:
                res = !ImmediateUnary(DmlUnary.Logical, subject);
                break;
            default:
                throw new DmlOperationNotImplemented();
        }

        return new VarEnvObjectReference(res, true);
    }
}