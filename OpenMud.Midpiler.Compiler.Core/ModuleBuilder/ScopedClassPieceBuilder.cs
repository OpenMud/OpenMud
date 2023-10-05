namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public class ScopedClassPieceBuilder : IModulePieceBuilder
{
    private readonly IModulePieceBuilder inner;
    private readonly string parent;

    public ScopedClassPieceBuilder(string parent, IModulePieceBuilder inner)
    {
        this.parent = parent;
        this.inner = inner;
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        inner.Visit(new ScopedDreamMakerSymbolResolver(parent, resolver));
    }
}