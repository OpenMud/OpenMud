using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class ImplicitReturnRewriter : CSharpSyntaxRewriter
{
    private MethodDeclarationSyntax ResolveEmptyReturns(MethodDeclarationSyntax src, bool useNull)
    {
        return src.WithBody(
            src.Body.ReplaceNodes(
                src.DescendantNodes().Where(
                        x => x is ReturnStatementSyntax r &&
                             r.Expression == null)
                    .Cast<ReturnStatementSyntax>(),
                (n, m) => SyntaxFactory.ReturnStatement(
                    useNull
                        ? SyntaxFactory.LiteralExpression(
                            SyntaxKind.NullLiteralExpression
                        )
                        : SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ThisExpression()
                                .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation),
                            SyntaxFactory.IdentifierName("ImplicitReturn")
                        )
                )
            )
        );
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax src)
    {
        if (src.HasAnnotation(BuilderAnnotations.UnmanagedReturnValue))
            return src;

        var anyImplicitReturnAssignment = src.Body.DescendantNodes()
            .Any(x => x.HasAnnotation(BuilderAnnotations.ImplicitReturnAssignment));

        var hasReturn = src.Body.Statements.LastOrDefault() is ReturnStatementSyntax;

        if (!hasReturn)
            src = src.WithBody(
                src.Body.AddStatements(
                    SyntaxFactory.ReturnStatement()
                )
            );

        return ResolveEmptyReturns(src, !anyImplicitReturnAssignment);
    }
}