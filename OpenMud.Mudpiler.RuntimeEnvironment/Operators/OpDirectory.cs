using System.Collections.Immutable;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public class OpDirectory : IOpSolver
{
    private static readonly IImmutableSet<Type> primitiveTypes = new[]
    {
        typeof(int),
        typeof(double),
        typeof(string),
        typeof(bool)
    }.ToImmutableHashSet();

    private readonly IOpSolver nullSolver = new OpNullSolver();
    private readonly IOpSolver primitiveSolver = new OpPrimitive();
    private readonly ExecuteTransaction setupExecutor;

    public OpDirectory(ExecuteTransaction executor)
    {
        setupExecutor = executor;
    }

    public DatumProcExecutionContext Binary(DmlBinary op, EnvObjectReference subject, EnvObjectReference operand)
    {
        return GetSubjectSolver(subject).Binary(op, subject, operand);
    }

    public DatumProcExecutionContext BinaryAssignment(DmlBinaryAssignment op, EnvObjectReference subject,
        EnvObjectReference operand)
    {
        return GetSubjectSolver(subject).BinaryAssignment(op, subject, operand);
    }

    public DatumProcExecutionContext Unary(DmlUnary op, EnvObjectReference subject)
    {
        return GetSubjectSolver(subject).Unary(op, subject);
    }

    public DatumProcExecutionContext UnaryAssignment(DmlUnaryAssignment op, EnvObjectReference subject)
    {
        return GetSubjectSolver(subject).UnaryAssignment(op, subject);
    }

    public DatumProcExecutionContext Ternery(DmlTernery op, EnvObjectReference subject, EnvObjectReference op0,
        EnvObjectReference op1)
    {
        return GetSubjectSolver(subject).Ternery(op, subject, op0, op1);
    }

    private bool IsPrimitive(Type obj)
    {
        return primitiveTypes.Contains(obj);
    }

    private IOpSolver GetSubjectSolver(EnvObjectReference obj)
    {
        if (obj.Target == null)
            return nullSolver;
        if (IsPrimitive(obj.Type))
            return primitiveSolver;
        if (obj.Target is Datum d)
            return CreateObjectSolver(setupExecutor, d);
        return new OpNative();
    }

    private static List<Tuple<string, T>> GetOverrides<T>(Datum t) where T : IDmlProcAttribute
    {
        return t
            .EnumerateProcs()
            .Where(x => x.HasAttribute<T>())
            .Select(m =>
                (method: m, attr: m.GetAttributes<T>())
            )
            .SelectMany(x => x.attr.Select(y => Tuple.Create(x.method.Name, y)))
            .ToList();
    }

    private static IOpSolver CreateObjectSolver(ExecuteTransaction executor, Datum t)
    {
        var solver = new OpObject();

        var binOverrides = GetOverrides<BinOpOverride>(t);
        var unOverrides = GetOverrides<UnOpOverride>(t);
        var binAsnOverrides = GetOverrides<BinOpAsnOverride>(t);
        var unAsnOverrides = GetOverrides<UnOpAsnpOverride>(t);
        var ternOpOverides = GetOverrides<TernOpAsnpOverride>(t);

        DatumProcExecutionContext simpleExecutor(EnvObjectReference subject, string name,
            params EnvObjectReference[] operands)
        {
            var r = executor.Invoke(null, null, subject, new ProcArgumentList(operands), name);

            r.UnmanagedContinue();

            return r;
        }

        ;

        foreach (var op in binOverrides)
            solver.Override(op.Item2.Operation, (subject, operand) => simpleExecutor(subject, op.Item1, operand));

        foreach (var op in unOverrides)
            solver.Override(op.Item2.Operation, subject => simpleExecutor(subject, op.Item1));

        foreach (var op in binAsnOverrides)
            solver.Override(op.Item2.Operation, (subject, operand) => simpleExecutor(subject, op.Item1, operand));

        foreach (var op in ternOpOverides)
            solver.Override(op.Item2.Operation, (subject, op0, op1) => simpleExecutor(subject, op.Item1, op0, op1));

        foreach (var op in unAsnOverrides)
        foreach (var attr in op.Item2.Operations)
        {
            var isPost = DmlOperation.IsPost(attr);
            solver.Override(attr,
                subject => simpleExecutor(subject, op.Item1, new VarEnvObjectReference(isPost ? 1 : 0)));
        }

        return solver;
    }

    public DatumProcExecutionContext PrimitiveCast(EnvObjectReference subject, EnvObjectReference type)
    {
        var solver = GetSubjectSolver(subject);

        return solver.PrimitiveCast(subject, type);
    }
}