using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class IsTypeResolverRewriter : CSharpSyntaxRewriter
{
    private readonly Stack<Dictionary<string, string>> knownDeclarations = new();
    private readonly Func<string, ClassDeclarationSyntax> lookupCls;
    private readonly Func<ClassDeclarationSyntax, string> lookupName;

    public IsTypeResolverRewriter(Func<string, ClassDeclarationSyntax> lookupCls,
        Func<ClassDeclarationSyntax, string> lookupName)
    {
        this.lookupCls = lookupCls;
        this.lookupName = lookupName;
    }

    private Dictionary<string, string> FindBlockDeclarations(BlockSyntax c)
    {
        var descNodes = c.DescendantNodes(n =>
            !(n is BlockSyntax && n != c)
        );

        var explicitDecl =
            descNodes.Where(n => n is VariableDeclaratorSyntax)
                .Cast<VariableDeclaratorSyntax>()
                .ToDictionary(
                    n => n.Identifier.Text,
                    n => BuilderAnnotations.ExtractTypeHintAnnotationOrDefault(n));

        return explicitDecl;
    }

    private Dictionary<string, string> FindClassDeclarations(ClassDeclarationSyntax c)
    {
        if (BuilderAnnotations.HasDmlProcClassAnnotation(c))
            c = lookupCls(BuilderAnnotations.GetDmlProcClassAnnotation(c));

        var decl = c.DescendantNodes(n =>
                !(n is BlockSyntax)
            )
            .Where(n => n is VariableDeclaratorSyntax)
            .Cast<VariableDeclaratorSyntax>()
            .ToDictionary(
                n => n.Identifier.Text,
                n => BuilderAnnotations.ExtractTypeHintAnnotationOrDefault(n));

        var fullPath = lookupName(c);
        var baseClass = DmlPath.ResolveParentPath(fullPath);

        if (baseClass != null && !DmlPath.IsRoot(baseClass))
            decl = decl.Concat(FindClassDeclarations(lookupCls(baseClass))).ToDictionary(x => x.Key, x => x.Value);

        return decl;
    }


    private string InferType(string name)
    {
        foreach (var n in knownDeclarations)
            if (n.TryGetValue(name, out var decl))
                return decl;

        throw new Exception("No type hint!");
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var classDecl = FindClassDeclarations(node);
        knownDeclarations.Push(classDecl);

        var r = base.VisitClassDeclaration(node);

        knownDeclarations.Pop();

        return r;
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var argTypes = node
            .ParameterList
            .DescendantNodes()
            .Where(x => x is ParameterSyntax)
            .Cast<ParameterSyntax>()
            .ToDictionary(
                p => p.Identifier.Text,
                p => BuilderAnnotations.ExtractTypeHintAnnotationOrDefault(p)
            );

        knownDeclarations.Push(argTypes);

        var r = base.VisitMethodDeclaration(node);

        knownDeclarations.Pop();

        return r;
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (BuilderAnnotations.HasIsTypeAnnotationWithoutType(node))
            return SyntaxFactory.InvocationExpression(
                    node.Expression,
                    node.ArgumentList.WithArguments(
                        SyntaxFactory.SeparatedList(
                            node.ArgumentList.Arguments.Append(
                                SyntaxFactory.Argument(
                                    ExpressionVisitor.CreateResolveType(
                                        InferType(BuilderAnnotations.GetIsTypeTarget(node))))
                            )
                        )
                    )
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
        if (BuilderAnnotations.HasInferNewTypeTarget(node))
            return SyntaxFactory.InvocationExpression(
                    node.Expression,
                    node.ArgumentList.WithArguments(
                        SyntaxFactory.SeparatedList(
                            node.ArgumentList.Arguments.Prepend(
                                SyntaxFactory.Argument(
                                    ExpressionVisitor.CreateResolveType(
                                        InferType(BuilderAnnotations.GetInferNewTypeTarget(node))))
                            )
                        )
                    )
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
        return base.VisitInvocationExpression(node);
    }

    public override SyntaxNode? VisitBlock(BlockSyntax c)
    {
        var blockDecl = FindBlockDeclarations(c).ToDictionary(x => x.Key, x => x.Value);

        knownDeclarations.Push(blockDecl);

        var r = base.VisitBlock(c);

        knownDeclarations.Pop();

        return r;
    }
}