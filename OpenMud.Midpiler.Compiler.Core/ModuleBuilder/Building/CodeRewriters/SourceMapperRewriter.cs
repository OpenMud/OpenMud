using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters
{
    internal class SourceMapperRewriter : CSharpSyntaxRewriter
    {
        private Stack<StatementSyntax> statements = new();
        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            // Check if this is a statement or a statement-like construct.
            if (node is StatementSyntax stmt && BuilderAnnotations.GetSourceMap(node, out var file, out var line))
            {
                
                var trivia = SyntaxFactory.Trivia(
                    SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Literal(line), SyntaxFactory.Literal(file), true)
                );
                node = node.WithLeadingTrivia(trivia);
            }

            // Visit children nodes recursively.
            return base.Visit(node);
        }
    }
}
