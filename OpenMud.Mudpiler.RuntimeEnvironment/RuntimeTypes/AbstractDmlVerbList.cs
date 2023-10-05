using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public abstract class AbstractDmlVerbList : DmlList
{
    public abstract VerbMetadata[] AvailableVerbs { get; }
    public abstract void Register(DatumProc d, string? name, string? description);
    public abstract void SetRegisteredProcedures(DatumProcCollection registedProcedures);
    public abstract void SetDefaultVerbSource(VerbSrc verbSrc);
    public abstract void AddOrigin(EnvObjectReference verb);
}