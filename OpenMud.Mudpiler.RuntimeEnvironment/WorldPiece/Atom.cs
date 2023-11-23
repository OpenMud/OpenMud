using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

public class Atom : Datum
{
    public readonly EnvObjectReference Container = VarEnvObjectReference.Variable(null);
    public readonly EnvObjectReference contents = VarEnvObjectReference.Variable(null);
    public readonly EnvObjectReference density = VarEnvObjectReference.Variable(0);
    public readonly EnvObjectReference desc = VarEnvObjectReference.Variable("");
    public readonly ManagedEnvObjectReference dir = new();
    public readonly EnvObjectReference gender = VarEnvObjectReference.Variable("neutral");
    public readonly EnvObjectReference icon = VarEnvObjectReference.Variable("");
    public readonly EnvObjectReference icon_state = VarEnvObjectReference.Variable("");
    public readonly ManagedEnvObjectReference layer = new();

    public readonly dynamic loc = new ManagedEnvObjectReference();
    public readonly EnvObjectReference luminosity = VarEnvObjectReference.Variable(0);
    public readonly EnvObjectReference name = VarEnvObjectReference.Variable(null);
    public readonly EnvObjectReference opacity = VarEnvObjectReference.Variable(0);
    public readonly EnvObjectReference suffix = VarEnvObjectReference.Variable("");
    public readonly EnvObjectReference text = VarEnvObjectReference.Variable(null);

    public readonly EnvObjectReference verbs = VarEnvObjectReference.Variable(null);
    public readonly EnvObjectReference visibility = VarEnvObjectReference.Variable(1);
    public readonly ManagedEnvObjectReference x = new();
    public readonly ManagedEnvObjectReference y = new();
    private AbstractDmlVerbList _verbs;

    public VerbMetadata[] Verbs => _verbs.AvailableVerbs;

    public event Action? VerbsChanged;
    public event Action? ContentsChanged;
    public event Action? PropertiesChanged;
    public event Action? ContainerChanged;
    public event Action? IconChanged;
    public event Action? IconStateChanged;

    public override void SetContext(DatumExecutionContext ctx)
    {
        base.SetContext(ctx);

        contents.Assign(VarEnvObjectReference.CreateImmutable(ctx.NewAtomic("/list/exclusive")), true);
        verbs.Assign(VarEnvObjectReference.CreateImmutable(ctx.NewAtomic("/list/verb")), true);

        _verbs = verbs.Get<AbstractDmlVerbList>();
        _verbs.SetRegisteredProcedures(RegistedProcedures);

        var typePath = type.Get<string>();
        if (DmlPath.IsDeclarationInstanceOfPrimitive(typePath, DmlPrimitive.Mob))
            _verbs.SetDefaultVerbSource(new VerbSrc(SourceType.User));
        else if (DmlPath.IsDeclarationInstanceOfPrimitive(typePath, DmlPrimitive.Obj))
            _verbs.SetDefaultVerbSource(new VerbSrc(SourceType.UserContents));
        else if (DmlPath.IsDeclarationInstanceOfPrimitive(typePath, DmlPrimitive.Turf) ||
                 DmlPath.IsDeclarationInstanceOfPrimitive(typePath, DmlPrimitive.Area))
            _verbs.SetDefaultVerbSource(new VerbSrc(SourceType.View, 0));


        _verbs.Changed += () => VerbsChanged?.Invoke();
        contents.Get<DmlList>().Changed += () => ContentsChanged?.Invoke();
        name.Assigned += () => PropertiesChanged?.Invoke();
        gender.Assigned += () => PropertiesChanged?.Invoke();
        desc.Assigned += () => PropertiesChanged?.Invoke();
        suffix.Assigned += () => PropertiesChanged?.Invoke();
        text.Assigned += () => PropertiesChanged?.Invoke();
        visibility.Assigned += () => PropertiesChanged?.Invoke();
        luminosity.Assigned += () => PropertiesChanged?.Invoke();
        density.Assigned += () => PropertiesChanged?.Invoke();
        opacity.Assigned += () => PropertiesChanged?.Invoke();
        Container.Assigned += () => ContainerChanged?.Invoke();

        icon.Assigned += () => IconChanged?.Invoke();
        icon_state.Assigned += () => IconStateChanged?.Invoke();
    }

    public void RegisterExternalVerb(DatumProc d, string? name, string? description)
    {
        _verbs.Register(d, name, description);
    }

    protected override void DoRegisterProcedure(long prec, DatumProc procedure)
    {
        base.DoRegisterProcedure(prec, procedure);

        if (RegistedProcedures.GetAttributes(procedure.Name, prec).Any(t => t is Verb))
            _verbs.AddOrigin(VarEnvObjectReference.CreateImmutable(procedure));
    }
}