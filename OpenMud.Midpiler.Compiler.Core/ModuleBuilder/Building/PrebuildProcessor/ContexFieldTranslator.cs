using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.PrebuildProcessor;

internal class ContexFieldTranslator : IClassPrebuildProcessor
{
    // Some special objects have hidden fields that need to be accounted for as well...
    private readonly HashSet<string> contextFields = new()
    {
        "src", "usr"
    };

    public void Process(string fullName, IDreamMakerSymbolResolver cls)
    {
        foreach (var m in cls.MethodDeclarations.Where(d => DmlPath.IsImmediateChild(fullName, d.Name)))
            cls.DefineClassMethod(m.Name, ResolveMethodVariableReferences, m.DeclarationOrder);
    }

    private ExpressionSyntax GenerateContextGetter(IdentifierNameSyntax originate)
    {
        if (originate.Identifier.Text == "src")
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                SyntaxFactory.IdentifierName("self")
            );

        return
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                originate.WithAdditionalAnnotations(BuilderAnnotations.SkipScopeResolution)
            );
    }

    private IEnumerable<IdentifierNameSyntax> FindBlockVariableAssignmentUses(BlockSyntax c)
    {
        var assignments = c.DescendantNodes(n =>
                !(n is BlockSyntax && n != c) &&
                !(n is MemberAccessExpressionSyntax)
            )
            .Where(n => n is AssignmentExpressionSyntax)
            .Cast<AssignmentExpressionSyntax>();

        var obviousUses = assignments.Select(
            a => a.DescendantNodesAndSelf().Where(n => n is IdentifierNameSyntax).Cast<IdentifierNameSyntax>()
        ).SelectMany(n => n).ToList();

        return obviousUses;
    }

    private BlockSyntax ResolveBlockVariableReference(BlockSyntax c)
    {
        var unqualifiedNames = c.DescendantNodes(n => !(n is QualifiedNameSyntax));
        var qualifiedNames = c.DescendantNodes(n => n is QualifiedNameSyntax)
            .SelectMany(n => ((QualifiedNameSyntax)n).Left.DescendantNodes());

        var expressionUses = unqualifiedNames.Concat(qualifiedNames)
            .Where(x => x is IdentifierNameSyntax)
            .Cast<IdentifierNameSyntax>()
            .Concat(FindBlockVariableAssignmentUses(c))
            .Where(x => contextFields.Contains(x.Identifier.Text))
            .ToList(); //Only want name identifiers


        return c.ReplaceNodes(expressionUses, (n, s) => GenerateContextGetter(n));
    }

    private MethodDeclarationSyntax ResolveMethodVariableReferences(MethodDeclarationSyntax c)
    {
        return c.WithBody(ResolveBlockVariableReference(c.Body));
    }
}