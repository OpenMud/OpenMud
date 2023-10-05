using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class LiteralObjRefRewriter : CSharpSyntaxRewriter
{
    private int blockDepth;

    private ExpressionSyntax GenerateLiteralWrapper(ExpressionSyntax originate)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(
                    typeof(VarEnvObjectReference).FullName
                ),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName("Wrap")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(originate)
                })
            )
        );
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.HasAnnotation(BuilderAnnotations.DontWrapAnnotation))
            return node;

        return base.VisitMethodDeclaration(node);
    }

    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        blockDepth++;
        var r = base.VisitBlock(node);
        blockDepth--;

        return r;
    }

    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (blockDepth <= 0)
            return base.VisitLiteralExpression(node);

        if (node.HasAnnotation(BuilderAnnotations.DontWrapAnnotation))
            return node;

        return GenerateLiteralWrapper((LiteralExpressionSyntax)base.VisitLiteralExpression(node));
    }

    public override SyntaxNode? VisitThisExpression(ThisExpressionSyntax node)
    {
        if (blockDepth <= 0)
            return base.VisitThisExpression(node);

        if (node.HasAnnotation(BuilderAnnotations.DontWrapAnnotation))
            return node;

        return GenerateLiteralWrapper((ThisExpressionSyntax)base.VisitThisExpression(node));
    }
}