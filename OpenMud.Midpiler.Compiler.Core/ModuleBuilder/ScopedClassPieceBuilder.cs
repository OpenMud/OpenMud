namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public class ScopedClassPieceBuilder : IModulePieceBuilder
{
    private readonly IModulePieceBuilder inner;
    private readonly string parent;

    public ScopedClassPieceBuilder(string parent, IModulePieceBuilder inner)
    {
        this.parent = parent;
        this.inner = inner;

        if (inner == null)
            throw new ArgumentException("Inner cannot be null.");
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        inner.Visit(new ScopedDreamMakerSymbolResolver(parent, resolver));
    }
}