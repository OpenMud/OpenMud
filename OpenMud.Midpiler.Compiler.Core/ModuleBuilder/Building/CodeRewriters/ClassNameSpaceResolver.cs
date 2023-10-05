using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class ClassNamespaceResolverWalker : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var classNamespace = SyntaxFactory.ParseName(CSharpModule.ROOT_NAMESPACE);

        return SyntaxFactory.NamespaceDeclaration(
            classNamespace,
            new SyntaxList<ExternAliasDirectiveSyntax>(),
            new SyntaxList<UsingDirectiveSyntax>(),
            new SyntaxList<MemberDeclarationSyntax>(new[] { node })
        );
    }
}