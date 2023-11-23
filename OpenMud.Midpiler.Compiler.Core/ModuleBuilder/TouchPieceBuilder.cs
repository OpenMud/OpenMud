using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public class TouchPieceBuilder : IModulePieceBuilder
{
    private readonly string parent;

    public TouchPieceBuilder(string parent)
    {
        this.parent = parent;
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        //In case we have trailing modifiers. For example obj/a/b/static, we just want to touch obj/a/b
        var touchTarget = DmlPath.RemoveTrailingModifiers(parent);
        resolver.Touch(touchTarget);
    }
}