using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public delegate ExpressionSyntax FieldInitBodyPieceBuilder(IDreamMakerSymbolResolver resolver);

public class FieldInitPieceBuilder : IModulePieceBuilder
{
    private readonly FieldInitBodyPieceBuilder init;
    private readonly string path;

    public FieldInitPieceBuilder(string path, FieldInitBodyPieceBuilder init)
    {
        this.path = path;
        this.init = init;
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        resolver.DefineFieldInitializer(path, init(resolver));
    }
}