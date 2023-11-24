using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

public abstract class DmlDatumProcExecutionContext : DatumProcExecutionContext
{
    private ProcArgumentList arglist = new();

    private string[] ArgumentNames = new string[0];

    protected int currentStep;

    private EnvObjectReference result = VarEnvObjectReference.NULL;

    public override ProcArgumentList ActiveArguments
    {
        get => arglist;
        set
        {
            arglist = value;
            SetupPositionalArguments(value.MapArguments(ArgumentNames));
        }
    }

    public override DatumProcExecutionContext Caller { get; set; }
    public override DatumExecutionContext ctx { get; set; }
    public override long precedence { get; set; }
    public override dynamic self { get; set; }
    public override dynamic usr { get; set; }

    public override EnvObjectReference Result => result;

    protected abstract void SetupPositionalArguments(object[] args);

    internal void DefineArgumentNames(string[] argumentNames)
    {
        ArgumentNames = argumentNames;
    }

    protected abstract object DoContinue();

    protected abstract DmlDatumProcExecutionContext DmlGenerateClone();

    protected override DatumProcExecutionContext GenerateClone(DatumProcExecutionContext newCallerCtx)
    {
        var dmlClone = DmlGenerateClone();
        dmlClone.currentStep = currentStep;
        dmlClone.ArgumentNames = ArgumentNames.ToArray();
        dmlClone.result = VarEnvObjectReference.CreateImmutable(result);
        dmlClone.Caller = newCallerCtx;

        return dmlClone;
    }

    protected override void ContinueHandler()
    {
        result = VarEnvObjectReference.CreateImmutable(DoContinue());
        State = DatumProcExecutionState.Completed;
    }
}

public abstract class DmlDatumProc : DatumProc
{
    public abstract string[] ArgumentNames();

    protected virtual DatumProcExecutionContext DmlCreateArgumentBuilder()
    {
        return new PreparedDatumProcContext(() => VarEnvObjectReference.NULL);
    }

    public override DatumProcExecutionContext CreateDefaultArgumentListBuilder()
    {
        var intermediate = DmlCreateArgumentBuilder();

        var netProc = new PreparedChainDatumProcContext(
            intermediate,
            r => {
                var argList = r.IsNull ? new EnvObjectReference[0] : r.Get<EnvObjectReference[]>();
                var namedArgList = new ProcArgumentList(ArgumentNames().Zip(argList)
                    .Select((a) => new ProcArgument(a.First, a.Second)).ToArray());

                return new PreparedDatumProcContext(() => new VarEnvObjectReference(namedArgList, true));
            }
        );

        return netProc;
    }

    protected abstract DmlDatumProcExecutionContext DmlCreate();

    public override DatumProcExecutionContext Create()
    {
        var intermediate = DmlCreate();
        intermediate.DefineArgumentNames(ArgumentNames());
        return intermediate;
    }
}