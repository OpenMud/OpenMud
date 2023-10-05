using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class ImmediateEvaluationRewriter : CSharpSyntaxRewriter
{
    private bool rewriteImmediates;

    private SyntaxNode CreateExecuteDeferred(InvocationExpressionSyntax inner)
    {
        return
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    inner,
                    SyntaxFactory.IdentifierName("CompleteOrException")
                ),
                SyntaxFactory.ArgumentList()
            );
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        rewriteImmediates = node.HasAnnotation(BuilderAnnotations.DmlImmediateEvaluateMethod);
        var r = base.VisitMethodDeclaration(node);
        rewriteImmediates = false;

        return r;
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (rewriteImmediates && node.HasAnnotation(BuilderAnnotations.DmlNativeDeferred))
            return CreateExecuteDeferred((InvocationExpressionSyntax)base.VisitInvocationExpression(node));

        return base.VisitInvocationExpression(node);
    }
}