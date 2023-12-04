using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class ReservedKeywordVariableNameConflictResolver : CSharpSyntaxRewriter
{
    private static readonly HashSet<string> reserved = new HashSet<string>()
    {
        "event",
        "internal",
        "virtual",
        "const"
    };

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        var id = node.Identifier.Text;

        if (reserved.Contains(id))
            return node.WithIdentifier(SyntaxFactory.Identifier("@" + id));

        return node;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var id = node.Identifier.Text;

        if (reserved.Contains(id))
            return node.WithIdentifier(SyntaxFactory.Identifier("@" + id));

        return node;
    }
}