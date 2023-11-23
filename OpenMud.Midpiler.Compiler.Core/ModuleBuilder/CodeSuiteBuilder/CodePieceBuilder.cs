using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.CodeSuiteBuilder;

public delegate StatementSyntax[] CodePieceBuilder(IDreamMakerSymbolResolver resolver);

// Move to more approriate place later...
public delegate ExpressionSyntax ExpressionPieceBuilder(IDreamMakerSymbolResolver resolver);

public delegate ArgumentSyntax ArgumentPieceBuilder(IDreamMakerSymbolResolver resolver);

public static class CodePieceBuilderUtil
{
    public static StatementSyntax[] NullCodePieceBuilder(IDreamMakerSymbolResolver r) =>
        new StatementSyntax[0];
}