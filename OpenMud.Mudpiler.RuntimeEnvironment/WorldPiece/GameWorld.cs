using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

public class GameWorld : Datum
{
    public readonly EnvObjectReference contents = VarEnvObjectReference.Variable(null);
    public readonly EnvObjectReference map_format = VarEnvObjectReference.Variable(EnvironmentConstants.TOPDOWN_MAP);
    public readonly EnvObjectReference mob = VarEnvObjectReference.Variable("/mob");
    public readonly EnvObjectReference name = VarEnvObjectReference.Variable("A World");
    public readonly EnvObjectReference turf = VarEnvObjectReference.Variable("/turf");
    public readonly EnvObjectReference view = VarEnvObjectReference.Variable(10);

    public override void SetContext(DatumExecutionContext ctx)
    {
        base.SetContext(ctx);
        contents.Assign(VarEnvObjectReference.CreateImmutable(ctx.NewAtomic("/list/exclusive")));
    }
}