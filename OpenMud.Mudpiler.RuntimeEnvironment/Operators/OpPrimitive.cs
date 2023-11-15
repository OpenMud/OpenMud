using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public sealed class OpPrimitive : IOpSolver
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

                case DmlBinaryAssignment.AugAddition:
                    buffer = ImmediateBinary(DmlBinary.Addition, subject, operand);
                    break;
                case DmlBinaryAssignment.AugSubtraction:
                    buffer = ImmediateBinary(DmlBinary.Subtraction, subject, operand);
                    break;
                case DmlBinaryAssignment.AugMultiplication:
                    buffer = ImmediateBinary(DmlBinary.Multiplication, subject, operand);
                    break;
                case DmlBinaryAssignment.AugDivision:
                    buffer = ImmediateBinary(DmlBinary.Division, subject, operand);
                    break;
                case DmlBinaryAssignment.AugModulo:
                    buffer = ImmediateBinary(DmlBinary.Modulo, subject, operand);
                    break;
                case DmlBinaryAssignment.AugIntModulo:
                    buffer = ImmediateBinary(DmlBinary.IntModulo, subject, operand);
                    break;
                case DmlBinaryAssignment.AugBitwiseOr:
                    buffer = ImmediateBinary(DmlBinary.BitwiseOr, subject, operand);
                    break;
                case DmlBinaryAssignment.AugBitewiseAnd:
                    buffer = ImmediateBinary(DmlBinary.BitwiseAnd, subject, operand);
                    break;
                case DmlBinaryAssignment.AugBitwiseXor:
                    buffer = ImmediateBinary(DmlBinary.BitwiseXor, subject, operand);
                    break;
                case DmlBinaryAssignment.AugBitShiftLeft:
                    buffer = ImmediateBinary(DmlBinary.BitShiftLeft, subject, operand);
                    break;
                case DmlBinaryAssignment.AugBitShiftRight:
                    buffer = ImmediateBinary(DmlBinary.BitShiftRight, subject, operand);
                    break;
                default:
                    throw new DmlOperationNotImplemented();
            }

            var result = buffer;

            //Primitives might not be assignabe. For example in the expression:
            // 5 += 2; but we still want to evaluate y = 5 += 2 to 7
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
        return new PreparedDatumProcContext(() =>
        {
            dynamic subject = subjectRef.Target;
            object buffer;
            dynamic ret;

            switch (op)
            {
                case DmlUnaryAssignment.PreIncrement:
                    buffer = subject + 1;
                    ret = subject + 1;
                    break;
                case DmlUnaryAssignment.PreDecrement:
                    buffer = subject - 1;
                    ret = subject - 1;
                    break;
                case DmlUnaryAssignment.PostIncrement:
                    buffer = subject + 1;
                    ret = subject;
                    break;
                case DmlUnaryAssignment.PostDecrement:
                    buffer = subject - 1;
                    ret = subject;
                    break;
                default:
                    throw new DmlOperationNotImplemented();
            }

            if(subjectRef.IsAssignable)
                subjectRef.Assign(new VarEnvObjectReference(buffer));

            return new VarEnvObjectReference(ret, true);
        });
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
            case DmlBinary.Addition:
                res = subject + operand;
                break;
            case DmlBinary.Subtraction:
                res = subject - operand;
                break;
            case DmlBinary.Multiplication:
                res = subject * operand;
                break;
            case DmlBinary.Division:
                res = subject / operand;
                break;
            case DmlBinary.Modulo:
                res = subject % operand;
                break;
            case DmlBinary.IntModulo:
                res = subject % (int)operand;
                break;
            case DmlBinary.Power:
                res = Math.Pow(subject, operand);
                break;
            case DmlBinary.BitwiseOr:
                res = (int)subject | (int)operand;
                break;
            case DmlBinary.BitwiseAnd:
                res = (int)subject & (int)operand;
                break;
            case DmlBinary.BitwiseXor:
                res = (int)subject ^ (int)operand;
                break;
            case DmlBinary.BitShiftLeft:
                res = (int)subject << (int)operand;
                break;
            case DmlBinary.BitShiftRight:
                res = (int)subject >> (int)operand;
                break;
            case DmlBinary.Equivalent:
                res = subject == operand;
                break;
            case DmlBinary.NotEquivalent:
            case DmlBinary.NotEqual:
                res = !ImmediateBinary(DmlBinary.Equals, subjectRef, operandRef).Get<bool>();
                break;
            case DmlBinary.LessThan:
                res = subject < operand;
                break;
            case DmlBinary.GreaterThanEq:
                res = subject >= operand;
                break;
            case DmlBinary.GreaterThan:
                res = subject > operand;
                break;
            case DmlBinary.LessThanEq:
                res = subject <= operand;
                break;
            case DmlBinary.Equals:
                res = subject == operand;
                break;
            case DmlBinary.ArrayIndex:
                res = subject[(int)operand];
                break;
            case DmlBinary.Turn:
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
            case DmlUnary.Negate:
                res = -subject;
                break;
            case DmlUnary.BitwiseInvert:
                res = ~(int)subject;
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