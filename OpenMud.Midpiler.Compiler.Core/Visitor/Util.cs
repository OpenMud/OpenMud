using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;

namespace OpenMud.Mudpiler.Compiler.Core.Visitor;

internal static class Util
{
    public static IdentifierNameSyntax IdentifierName(string name)
    {
        return SyntaxFactory.IdentifierName(name)
            .WithAdditionalAnnotations(BuilderAnnotations.DmlCodeVariableAnnotation);
    }

    public static T DmlVariableIdentifier<T>(T name) where T : SyntaxNode
    {
        return name.WithAdditionalAnnotations(BuilderAnnotations.DmlCodeVariableAnnotation);
    }
}