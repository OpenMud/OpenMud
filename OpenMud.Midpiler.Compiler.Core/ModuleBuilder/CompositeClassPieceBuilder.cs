namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public class CompositeClassPieceBuilder : IModulePieceBuilder
{
    private readonly IModulePieceBuilder[] builders;

    public CompositeClassPieceBuilder(IEnumerable<IModulePieceBuilder> builders)
    {
        if (builders.Any(x => x == null))
            throw new ArgumentException("Builder cannot be null.");
        this.builders = builders.ToArray();
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        foreach (var b in builders)
            b.Visit(resolver);
    }
}