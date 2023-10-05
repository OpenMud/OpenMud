using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class GenerateLocalContextRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<string> classDecl = new();
    private bool isDatumProc;

    private (ISet<LocalDeclarationStatementSyntax> decls, ISet<ParameterSyntax> parameters, ISet<IdentifierNameSyntax>
        varUses) FindMethodDeclarations(MethodDeclarationSyntax c)
    {
        var parameterDeclaration = c.ParameterList.DescendantNodes().Where(x => x is ParameterSyntax)
            .Cast<ParameterSyntax>().ToHashSet();

        var explicitDecl = c.DescendantNodes()
            .Where(n => n is LocalDeclarationStatementSyntax)
            .Cast<LocalDeclarationStatementSyntax>()
            .ToHashSet();

        var varRefs = c.DescendantNodes()
            .Where(n => n is IdentifierNameSyntax && n.HasAnnotation(BuilderAnnotations.DmlCodeVariableAnnotation))
            .Where(n => !(n.Parent is MemberAccessExpressionSyntax y && n != y.Expression))
            .Cast<IdentifierNameSyntax>()
            .ToHashSet();

        return (explicitDecl, parameterDeclaration, varRefs);
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax cls)
    {
        isDatumProc = false;
        classDecl.Clear();

        cls = (ClassDeclarationSyntax)base.VisitClassDeclaration(cls);

        if (!isDatumProc)
            return cls;

        cls = cls.AddMembers(
            classDecl.Select(
                    d =>
                        SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("dynamic"),
                                SyntaxFactory.SeparatedList(new[]
                                    {
                                        SyntaxFactory.VariableDeclarator(d)
                                            .WithInitializer(
                                                SyntaxFactory.EqualsValueClause(
                                                    SyntaxFactory.ObjectCreationExpression(
                                                        SyntaxFactory.ParseTypeName(typeof(VarEnvObjectReference)
                                                            .FullName),
                                                        SyntaxFactory.ArgumentList(),
                                                        null
                                                    )
                                                )
                                            )
                                    }
                                )
                            )
                        )
                )
                .ToArray()
        );

        cls = cls.AddMembers(GenerateCloneBuilder(cls));

        classDecl.Clear();

        return cls;
    }

    private MemberDeclarationSyntax GenerateCloneBuilder(ClassDeclarationSyntax cls)
    {
        var creationExpr = SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.IdentifierName(cls.Identifier.Text),
            SyntaxFactory.ArgumentList(),
            null
        );

        var declarator = SyntaxFactory.VariableDeclarator("clonedCtx")
            .WithInitializer(SyntaxFactory.EqualsValueClause(creationExpr));

        var createCtxAssignment = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("dynamic"),
                SyntaxFactory.SeparatedList(new[]
                {
                    declarator
                })
            )
        );

        var returnCtx = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("clonedCtx"));

        var memberAssignmentExpressions = classDecl.Select(d =>
            (StatementSyntax)SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("clonedCtx"),
                        SyntaxFactory.IdentifierName(d)
                    ),
                    SyntaxFactory.ConditionalExpression(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            SyntaxFactory.IdentifierName(d),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                        ),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(d),
                                SyntaxFactory.IdentifierName("Clone")
                            )
                        )
                    )
                )
            )
        );

        var cloneBody = memberAssignmentExpressions
            .Prepend(createCtxAssignment)
            .Append(returnCtx)
            .ToList();

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName(typeof(DmlDatumProcExecutionContext).FullName),
                "DmlGenerateClone"
            )
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.List(cloneBody)
            ))
            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation)
            .WithAdditionalAnnotations(BuilderAnnotations.UnmanagedReturnValue);
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var decl = FindMethodDeclarations(node);

        isDatumProc = BuilderAnnotations.HasProcNameAnnotation(node);

        if (!isDatumProc || !decl.decls.Any())
            return base.VisitMethodDeclaration(node);

        var localVarDecl = SyntaxFactory.StructDeclaration("local_execution_context");

        foreach (var u in decl.decls)
            classDecl.Add("localvar_" + u.Declaration.Variables.Single().Identifier.Text);

        foreach (var u in decl.parameters)
            classDecl.Add("localvar_" + u.Identifier.Text);

        var argAssignmentStatements = decl.parameters.Select(u =>
            (StatementSyntax)SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ParseName("localvar_" + u.Identifier.Text),
                    SyntaxFactory.ParseName(u.Identifier.Text)
                )
            )
        );

        node = node.ReplaceNodes(
            decl.varUses.Cast<SyntaxNode>().Concat(decl.decls),
            (a, x) =>
            {
                if (x is IdentifierNameSyntax b)
                    return b.WithIdentifier(SyntaxFactory.Identifier("localvar_" + b.Identifier.Text));

                if (x is LocalDeclarationStatementSyntax c)
                {
                    var varDecl = c.Declaration.Variables.Single();

                    if (varDecl.Initializer == null)
                        return SyntaxFactory.EmptyStatement();

                    return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("localvar_" + varDecl.Identifier.Text),
                        varDecl.Initializer.Value
                    ));
                }

                throw new Exception();
            }
        );

        node = node.WithBody(
            node.Body.WithStatements(
                SyntaxFactory.List(
                    argAssignmentStatements.Concat(
                        node.Body.Statements
                    )
                )
            )
        );
        return base.VisitMethodDeclaration(node);
    }
}