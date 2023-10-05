namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public interface IModulePieceBuilder
{
    void Visit(IDreamMakerSymbolResolver resolver);
}

public class NullModulePieceBuilder : IModulePieceBuilder
{
    public void Visit(IDreamMakerSymbolResolver resolver)
    {
    }
}