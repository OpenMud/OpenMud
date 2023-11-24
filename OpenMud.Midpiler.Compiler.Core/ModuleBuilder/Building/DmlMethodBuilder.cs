using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;

public static class DmlMethodBuilder
{
    public static ClassDeclarationSyntax BuildMethodExecutionContextClass(MethodDeclarationSyntax e, string className,
        string name)
    {
        var decl = GenerateExecutionContextClassImplementation(e, className, name);

        return decl;
    }

    public static ClassDeclarationSyntax BuildDefaultArgListBuilderMethodClass(string name, string subjectClassName, MethodDeclarationSyntax subjectMethod)
    {
        var argInit = subjectMethod.ParameterList.Parameters.ToList().Select(p => p.Default?.Value).Select(e => e ?? ExpressionVisitor.CreateNull());

        var defaultResult = SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(typeof(EnvObjectReference).FullName),
            SyntaxFactory.List(new[] { SyntaxFactory.ArrayRankSpecifier() }));

        var argBuilderMethod = SyntaxFactory.MethodDeclaration(
               defaultResult,
               "ArgumentDefaults"
           )
           .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
               SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
           .WithBody(SyntaxFactory.Block(
               SyntaxFactory.List<StatementSyntax>(new[]
               {
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ArrayCreationExpression(
                            defaultResult,
                            SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                        argInit
                                    )
                                )
                        )
                    )
               })
           ))
           .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation);


        var r = GenerateExecutionContextClassImplementation(argBuilderMethod, null, name);

        return r.WithAdditionalAnnotations(BuilderAnnotations.CreateProcClassPathAnnotation(subjectClassName));
    }

    public static ClassDeclarationSyntax BuildMethodClass(string className, string name, int declarationOrder,
        MethodDeclarationSyntax e, ClassDeclarationSyntax contextImplementation, ClassDeclarationSyntax defaultArgListBuilder)
    {
        var createImpl = GenerateCreateContextImplementation(contextImplementation);

        var attr = GenerateAttributesGenerator(e);

        var namePropertyImpl = GenerateNameProperty(e.Identifier.Text);

        var implArgNames = e.ParameterList.Parameters.Select(p => p.Identifier.Text);

        var argNamesType = SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName("string"),
            SyntaxFactory.List(new[] { SyntaxFactory.ArrayRankSpecifier() }));
        var argNamesDecl = GenerateArgumentNamesImplementation(implArgNames, argNamesType);
        var argDefaultsDecl = GenerateArgumentDefaults(defaultArgListBuilder);

        var decl = GenerateDmlProcClassImplementation(className, name, createImpl, attr, argNamesDecl,
            namePropertyImpl, argDefaultsDecl);

        var classNames = DmlPath.EnumerateTypeAliasesOf(DmlPath.Concat(className, e.Identifier.Text), true);

        decl = decl.WithAttributeLists(
            SyntaxFactory.List(new[]
            {
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(
                        classNames.Select(clsName =>
                            SyntaxFactory.Attribute(
                                SyntaxFactory.ParseName(typeof(ProcDefinition).FullName),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(new[]
                                    {
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                SyntaxFactory.Literal(clsName)
                                            )
                                        ),
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                SyntaxFactory.Literal(declarationOrder)
                                            )
                                        )
                                    })
                                )
                            )
                        )
                    )
                )
            })
        );

        return decl;
    }

    private static PropertyDeclarationSyntax GenerateNameProperty(string procName)
    {
        // Create the string literal expression for the property value
        var propertyValue =
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(procName));

        // Create the property declaration syntax
        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
            default,
            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)),
            SyntaxFactory.ParseTypeName("string"),
            default,
            SyntaxFactory.Identifier("Name"),
            SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(propertyValue)))
                )
            )
        );

        return propertyDeclaration;
    }

    private static ClassDeclarationSyntax GenerateExecutionContextClassImplementation(MethodDeclarationSyntax e,
        string? className, string name)
    {
        var argFieldNames = e.ParameterList.Parameters.Select((p, i) => "argumentidx_" + i).ToArray();

        var argFields = e.ParameterList.Parameters.Select((p, i) =>
            {
                var field = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("dynamic"),
                        SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.VariableDeclarator("argumentidx_" + i)
                            }
                        )
                    )
                );

                return field;
            })
            .ToArray();

        var setupPosArgsMethod = GenerateSetupPositionalArguments(argFieldNames);
        var continueImpl = GenerateDmlContinue(e);

        var r = SyntaxFactory.ClassDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                SyntaxFactory.Identifier(name + "_context"),
                null,
                SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>(new[]
                {
                    SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.ParseTypeName(typeof(DmlDatumProcExecutionContext).FullName))
                })),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.List(new MemberDeclarationSyntax[] { setupPosArgsMethod, continueImpl }.Concat(argFields))
            );

        if(className != null)
            r = r.WithAdditionalAnnotations(BuilderAnnotations.CreateProcClassPathAnnotation(className));

        return r;
    }

    private static MethodDeclarationSyntax GenerateSetupPositionalArguments(string[] argFieldNames)
    {
        var argSetters = argFieldNames.Select((n, i) =>
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(n),
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName("args"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(i))
                                )
                            })
                        )
                    )
                )
            );
        });

        var attrType = SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName("object"),
            SyntaxFactory.List(new[] { SyntaxFactory.ArrayRankSpecifier() }));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName("void"),
                "SetupPositionalArguments"
            )
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("args"))
                            .WithType(attrType)
                    })
                )
            )
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.List<StatementSyntax>(argSetters)
            ))
            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation)
            .WithAdditionalAnnotations(BuilderAnnotations.UnmanagedReturnValue);
    }

    private static ClassDeclarationSyntax GenerateDmlProcClassImplementation(string className, string name,
        params MemberDeclarationSyntax[] members)
    {
        return SyntaxFactory.ClassDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                SyntaxFactory.Identifier(name),
                null,
                SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>(new[]
                    { SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeof(DmlDatumProc).FullName)) })),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.List(members)
            )
            .WithAdditionalAnnotations(BuilderAnnotations.CreateProcClassPathAnnotation(className));
    }

    private static MethodDeclarationSyntax GenerateArgumentNamesImplementation(IEnumerable<string> implArgNames,
        ArrayTypeSyntax argNamesType)
    {
        return SyntaxFactory.MethodDeclaration(
                argNamesType,
                "ArgumentNames"
            )
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.List<StatementSyntax>(new[]
                {
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ArrayCreationExpression(
                            argNamesType,
                            SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                    implArgNames.Select(e =>
                                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(e))
                                    )
                                ))
                        )
                    )
                })
            ))
            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation);
    }

    private static MethodDeclarationSyntax GenerateArgumentDefaults(ClassDeclarationSyntax ctxClass)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName(typeof(DmlDatumProcExecutionContext).FullName),
                "DmlCreateArgumentBuilder"
            )
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.List<StatementSyntax>(new[]
                {
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName(ctxClass.Identifier.Text),
                            SyntaxFactory.ArgumentList(),
                            null
                        )
                    )
                })
            ))
            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation);
    }

    private static MethodDeclarationSyntax GenerateAttributesGenerator(MethodDeclarationSyntax e)
    {
        var attrType = SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(typeof(IDmlProcAttribute).FullName),
            SyntaxFactory.List(new[] { SyntaxFactory.ArrayRankSpecifier() }));

        return SyntaxFactory.MethodDeclaration(
                attrType,
                "Attributes"
            )
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.List<StatementSyntax>(new[]
                {
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ArrayCreationExpression(
                            attrType,
                            SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                    e.AttributeLists.SelectMany(e => e.Attributes).Select(e =>
                                        SyntaxFactory.ObjectCreationExpression(
                                            e.Name,
                                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                                                e.ArgumentList == null
                                                    ? new ArgumentSyntax[0]
                                                    : e.ArgumentList.Arguments.Select(e =>
                                                        SyntaxFactory.Argument(e.Expression))
                                            )),
                                            null
                                        )
                                    )
                                ))
                        )
                    )
                })
            ))
            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation);
    }

    private static MethodDeclarationSyntax GenerateDmlContinue(MethodDeclarationSyntax e)
    {
        var argFieldNames = e.ParameterList.Parameters.Select((p, i) => Tuple.Create(p, i))
            .ToDictionary(
                x => x.Item1,
                x => "argumentidx_" + x.Item2
            );

        var argGetters = argFieldNames.Select(v =>
        {
            var argName = v.Key.Identifier.Text;

            var declarator = SyntaxFactory.VariableDeclarator(argName)
                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(v.Value)));

            if (BuilderAnnotations.ExtractTypeHintAnnotation(v.Key, out var typeHint))
                declarator = declarator.WithAdditionalAnnotations(BuilderAnnotations.CreateTypeHints(typeHint));

            return (StatementSyntax)SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName("dynamic"),
                    SyntaxFactory.SeparatedList(new[]
                    {
                        declarator
                    })
                )
            );
        });

        var doContinueImpl =
            e.WithIdentifier(SyntaxFactory.Identifier("DoContinue"))
                .WithBody(
                    e.Body.WithStatements(
                        SyntaxFactory.List(
                            argGetters.Concat(e.Body.Statements)
                        )
                    )
                )
                .WithReturnType(SyntaxFactory.ParseTypeName("object"))
                .WithParameterList(SyntaxFactory.ParameterList())
                .WithAdditionalAnnotations(BuilderAnnotations.ProcClassMethod)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
                .WithAdditionalAnnotations(BuilderAnnotations.CreateProcNameAnnotation(e.Identifier.Text));

        return doContinueImpl;
    }

    private static MethodDeclarationSyntax GenerateCreateContextImplementation(ClassDeclarationSyntax ctxClass)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName(typeof(DmlDatumProcExecutionContext).FullName),
                "DmlCreate"
            )
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.List<StatementSyntax>(new[]
                {
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName(ctxClass.Identifier.Text),
                            SyntaxFactory.ArgumentList(),
                            null
                        )
                    )
                })
            ))
            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation);
    }
}