using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class SelfSuperCallRewriter : CSharpSyntaxRewriter
{
    private Func<string, ClassDeclarationSyntax> lookupCls;
    private Func<ClassDeclarationSyntax, string> lookupName;

    public SelfSuperCallRewriter(Func<string, ClassDeclarationSyntax> lookupCls,
        Func<ClassDeclarationSyntax, string> lookupName)
    {
        this.lookupCls = lookupCls;
        this.lookupName = lookupName;
    }

    private List<ArgumentSyntax> InheritActiveArgumentList()
    {
        var args = new List<ArgumentSyntax>();

        args.Add(
            SyntaxFactory.Argument(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName("ActiveArguments")
                )
            )
        );

        return args;
    }

    private MethodDeclarationSyntax ResolveBaseCalls(MethodDeclarationSyntax src)
    {
        var srcInvokeName = BuilderAnnotations.HasProcNameAnnotation(src)
            ? BuilderAnnotations.GetProcNameAnnotation(src)
            : src.Identifier.Text;

        var priorPrecExpr = SyntaxFactory.BinaryExpression(
            SyntaxKind.SubtractExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                SyntaxFactory.IdentifierName("precedence")
            ),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1))
        );

        return src.ReplaceNodes(
            src.GetAnnotatedNodes(BuilderAnnotations.CallBaseAnnotation),
            (n, m) =>
            {
                var originalCall = (InvocationExpressionSyntax)n;

                if (originalCall.ArgumentList.Arguments.Count == 0)
                    return SyntaxFactory.InvocationExpression(
                            SyntaxFactory.ParseName("ctx.ex.InvokePrec"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    InheritActiveArgumentList()
                                        .Prepend(SyntaxFactory.Argument(priorPrecExpr))
                                        .Prepend(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(srcInvokeName))))
                                        .Prepend(
                                            SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.ParseName("GetExecutingContext"),
                                                SyntaxFactory.ArgumentList()))
                                        )
                                )
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                        .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
                return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName("ctx.ex.InvokePrec"),
                        SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    originalCall.ArgumentList.Arguments
                                        .Prepend(SyntaxFactory.Argument(priorPrecExpr))
                                        .Prepend(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(srcInvokeName))))
                                        .Prepend(
                                            SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.ParseName("GetExecutingContext"),
                                                SyntaxFactory.ArgumentList()))
                                        )
                                )
                            )
                            .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(3))
                    )
                    .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                    .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
            });
    }

    private MethodDeclarationSyntax ResolveSelfCalls(MethodDeclarationSyntax src)
    {
        var srcInvokeName = BuilderAnnotations.HasProcNameAnnotation(src)
            ? BuilderAnnotations.GetProcNameAnnotation(src)
            : src.Identifier.Text;

        return src.ReplaceNodes(
            src.GetAnnotatedNodes(BuilderAnnotations.CallSelfAnnotation),
            (n, m) =>
            {
                var originalCall = (InvocationExpressionSyntax)n;

                if (originalCall.ArgumentList.Arguments.Count == 0)
                    return SyntaxFactory.InvocationExpression(
                            SyntaxFactory.ParseName("ctx.ex.Invoke"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    InheritActiveArgumentList()
                                        .Prepend(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(srcInvokeName))))
                                        .Prepend(
                                            SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.ParseName("GetExecutingContext"),
                                                SyntaxFactory.ArgumentList()))
                                        )
                                )
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                        .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
                return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName("ctx.ex.Invoke"),
                        SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    originalCall.ArgumentList.Arguments
                                        .Prepend(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(srcInvokeName))))
                                        .Prepend(
                                            SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.ParseName("GetExecutingContext"),
                                                SyntaxFactory.ArgumentList()))
                                        )
                                )
                            )
                            .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(2))
                    )
                    .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                    .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
            });
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax m)
    {
        var working = ResolveSelfCalls(ResolveBaseCalls(m));

        return base.VisitMethodDeclaration(working);
    }
}