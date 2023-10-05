using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class GlobalVariableRewriter : CSharpSyntaxRewriter
{
    private static readonly BindingFlags FIELD_SEARCH_FLAGS = BindingFlags.Public | BindingFlags.NonPublic |
                                                              BindingFlags.FlattenHierarchy | BindingFlags.Instance;

    // This ... needs to be uhh ... refactored.
    private static readonly Func<Type, HashSet<string>> computeNames = t =>
        t.GetFields(FIELD_SEARCH_FLAGS).Select(f => f.Name)
            .Concat(t.GetProperties(FIELD_SEARCH_FLAGS).Select(f => f.Name)).ToHashSet();

    // Some special objects have hidden fields that need to be accounted for as well...
    private readonly Dictionary<string, HashSet<string>> hiddenFields = BuiltinTypes.PrimitiveClassNames.ToDictionary(
        x => x,
        x => computeNames(BuiltinTypes.ResolveType(x)));

    private readonly List<string> knownFieldDeclarations = new();

    private readonly Func<string, ClassDeclarationSyntax> lookupCls;
    private readonly Func<ClassDeclarationSyntax, string> lookupName;
    private readonly HashSet<string> parameterDiscovered = new();
    private readonly List<string> varDeclDiscovered = new();

    public GlobalVariableRewriter(Func<string, ClassDeclarationSyntax> lookupCls,
        Func<ClassDeclarationSyntax, string> lookupName)
    {
        this.lookupCls = lookupCls;
        this.lookupName = lookupName;
    }

    private ExpressionSyntax GenerateGlobalGetter(IdentifierNameSyntax originate)
    {
        return
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(
                    "ctx.global"
                ),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                originate
            );
    }

    private ExpressionSyntax GenerateSelfGetter(IdentifierNameSyntax originate)
    {
        return
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(
                        "self"
                    )
                ),
                originate
            );
    }

    private ISet<string> FindBlockDeclarations(BlockSyntax c)
    {
        var descNodes = c.DescendantNodes(n =>
            !(n is BlockSyntax && n != c)
        );

        var implicitDecl = descNodes.Where(n => n is AssignmentExpressionSyntax)
            .Cast<AssignmentExpressionSyntax>()
            .Select(a => a.Left)
            .SelectMany(a =>
                a.DescendantNodes()
                    .Where(x => x is IdentifierNameSyntax).Cast<IdentifierNameSyntax>()
                    .Select(n => n.Identifier.Text)
            );

        implicitDecl = implicitDecl.Concat(
            descNodes.Where(n => n is ForEachStatementSyntax)
                .Cast<ForEachStatementSyntax>()
                .Select(n => n.Identifier.Text)
        );

        var explicitDecl =
            descNodes.Where(n => n is VariableDeclaratorSyntax)
                .Cast<VariableDeclaratorSyntax>()
                .Select(x => x.Identifier.Text);

        return implicitDecl.Concat(explicitDecl).ToHashSet();
    }

    private ISet<string> FindClassDeclarations(ClassDeclarationSyntax c)
    {
        var decl = c.DescendantNodes(n =>
                !(n is BlockSyntax)
            ).Where(n => n is FieldDeclarationSyntax)
            .Cast<FieldDeclarationSyntax>()
            .Select(n => n.Declaration.Variables.Single().Identifier.Text);

        var fullPath = lookupName(c);
        var baseClass = DmlPath.ResolveParentPath(fullPath);

        if (baseClass != null && !DmlPath.IsRoot(baseClass))
            decl = decl.Concat(FindClassDeclarations(lookupCls(baseClass)));

        var normalized = DmlPath.RootClassName(fullPath);

        if (hiddenFields.TryGetValue(normalized, out var hidden))
            decl = decl.Concat(hidden);
        else
            decl = decl.Concat(hiddenFields["/datum"]);

        return decl.ToHashSet();
    }

    public override SyntaxNode? VisitBlock(BlockSyntax c)
    {
        var curScopeDeclarations = FindBlockDeclarations(c);
        varDeclDiscovered.AddRange(curScopeDeclarations);

        var allMethodScopeDeclarations = varDeclDiscovered.Union(parameterDiscovered).ToHashSet();
        var allClassScopeDeclarations = allMethodScopeDeclarations.Union(knownFieldDeclarations).ToHashSet();

        var allNames = c.DescendantNodes(n => !(n is BlockSyntax && n != c))
            .Where(x => x is IdentifierNameSyntax)
            .Where(x => !BuilderAnnotations.SkipGlobalResolution(x))
            .Cast<IdentifierNameSyntax>().ToList();

        var globalExpressionUses = allNames
            .Where(x => !allClassScopeDeclarations.Contains(x.Identifier.Text))
            .ToList(); //Only want name identifiers

        var fieldExpressionUses = allNames
            .Where(x => !allMethodScopeDeclarations.Contains(x.Identifier.Text))
            .Except(globalExpressionUses)
            .ToList(); //Only want name identifiers

        var r = c.ReplaceNodes(globalExpressionUses.Concat(fieldExpressionUses),
            (n, s) => { return globalExpressionUses.Contains(n) ? GenerateGlobalGetter(n) : GenerateSelfGetter(n); });

        r = (BlockSyntax)base.VisitBlock(r);

        foreach (var d in curScopeDeclarations)
            varDeclDiscovered.Remove(d);

        return r;
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax c)
    {
        parameterDiscovered.UnionWith(c.ParameterList.DescendantNodes().Where(x => x is ParameterSyntax)
            .Cast<ParameterSyntax>().Select(p => p.Identifier.Text));

        var r = base.VisitMethodDeclaration(c);

        parameterDiscovered.Clear();
        return r;
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (BuilderAnnotations.IsRootGlobal(node))
            return node;

        var parentClassDecl = node;

        if (BuilderAnnotations.HasDmlProcClassAnnotation(node))
        {
            var parentClassName = BuilderAnnotations.GetDmlProcClassAnnotation(node);

            parentClassDecl = lookupCls(parentClassName);
        }

        var discoveredDecl = FindClassDeclarations(parentClassDecl);

        knownFieldDeclarations.AddRange(discoveredDecl);
        var r = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

        foreach (var d in discoveredDecl)
            knownFieldDeclarations.Remove(d);

        return r;
    }
}