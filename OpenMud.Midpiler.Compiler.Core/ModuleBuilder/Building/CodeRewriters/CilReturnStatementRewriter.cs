using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

//We need to rewrite the return statements so unreachable code isn't automatically removed by the C# compiler's static code analysis
//this code could be referenced via fodly InlineIL goto clauses. So we do not want it to be removed.
internal class CilReturnStatementRewriter : CSharpSyntaxRewriter
{
    private class ReturnRewriter : CSharpSyntaxRewriter
    {
        private bool enteredTopLevel = false;

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (enteredTopLevel)
                return node;

            enteredTopLevel = true;
            var r = base.VisitMethodDeclaration(node);
            enteredTopLevel = false;

            return r;
        }

        public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            return node;
        }

        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            //all return nodes should have a value (enforced by the ImplicitReturnRewriter pass)
            var retExpr = node.Expression!;

            retExpr = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("object"), retExpr);

            var push = SyntaxFactory.ExpressionStatement(
                SyntaxFactory
                .InvocationExpression(
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("InlineIL.IL.Push")
                    )
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.ObjectKeyword)
                                )
                            )
                        )
                    )
                )
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(retExpr)
                        )
                    )
                )
            );

            var retStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression($"InlineIL.IL.Emit.Ret()"));

            return SyntaxFactory.Block(new StatementSyntax[] {
            push,
            retStatement
        });
        }
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!BuilderAnnotations.HasProcNameAnnotation(node))
            return node;

        var r = (MethodDeclarationSyntax)new ReturnRewriter().Visit(node);

        var throwUnreachable = SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression($"throw InlineIL.IL.Unreachable()"));

        return r.WithBody(
            r.Body.WithStatements(SyntaxFactory.List<StatementSyntax>(
                    r.Body.Statements.Append(throwUnreachable )
                )
            )
        );
    }
}