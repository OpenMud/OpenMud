using Antlr4.Runtime.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.CodeSuiteBuilder;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Compiler.Core.Visitor;

public struct VariableDeclarationMetadata
{
    public string variableName;
    public string objectType;
    public ExpressionPieceBuilder init;
    public TypeSyntax varType;
}

public class CodeSuiteVisitor : DmlParserBaseVisitor<CodePieceBuilder>
{
    private static readonly string VARGEN_PREFIX = "__supportvar";
    private readonly ExpressionVisitor EXPR = new();
    private readonly SourceMapping mapping;

    private int vargenIndex;

    internal CodeSuiteVisitor(SourceMapping mapping)
    {
        this.mapping = mapping;
    }

    public IdentifierNameSyntax GenerateSupportVariable()
    {
        return Util.IdentifierName(VARGEN_PREFIX + vargenIndex++);
    }

    public override CodePieceBuilder VisitSpawn_stmt([NotNull] DmlParser.Spawn_stmtContext context)
    {
        return builder =>
        {
            var spawnCall = EXPR.CreateCall("spawn", new[]
            {
                SyntaxFactory.Argument(EXPR.Visit(context.delay)(builder))
            });

            return SyntaxFactory.IfStatement(
                spawnCall,
                SyntaxFactory.Block(
                    SyntaxFactory.List(new []
                    {
                        Visit(context.suite())(builder),
                        SyntaxFactory.ReturnStatement()
                    })
                )
            );
        };
    }

    public override CodePieceBuilder VisitIf_stmt(DmlParser.If_stmtContext c)
    {
        var last = c.else_pass == null ? null : Visit(c.else_pass);

        var chains = new List<Tuple<ExpressionPieceBuilder, CodePieceBuilder>>();

        for (var i = 0; i < c.expr().Length; i++)
            chains.Add(new Tuple<ExpressionPieceBuilder, CodePieceBuilder>(EXPR.Visit(c.expr(i)), Visit(c.suite(i))));

        return resolver =>
        {
            var lastBuilt = last == null ? null : last(resolver);

            for (var i = chains.Count - 1; i >= 0; i--)
                if (last == null)
                    lastBuilt = SyntaxFactory.IfStatement(ExpressionVisitor.Logical(chains[i].Item1)(resolver),
                        chains[i].Item2(resolver));
                else
                    lastBuilt = SyntaxFactory.IfStatement(ExpressionVisitor.Logical(chains[i].Item1)(resolver),
                        chains[i].Item2(resolver), SyntaxFactory.ElseClause(lastBuilt));

            return lastBuilt;
        };
    }


    public override CodePieceBuilder VisitForlist_list_recycle_in(
        [NotNull] DmlParser.Forlist_list_recycle_inContext context)
    {
        var iterName = Util.IdentifierName(context.iter_var.GetText());

        return resolver =>
        {
            var iteratorSupportName = GenerateSupportVariable();
            var body = Visit(context.suite())(resolver);

            var assignIterToVar = SyntaxFactory.ExpressionStatement(
                EXPR.CreateAssignment(
                    iterName,
                    iteratorSupportName
                )
            );

            //Add a istype filter...
            var filterIfStmnt = SyntaxFactory.IfStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    EXPR.CreateImplicitIsType(
                        iterName
                    )
                ),
                SyntaxFactory.ContinueStatement(),
                SyntaxFactory.ElseClause(body)
            );

            body = SyntaxFactory.Block(SyntaxFactory.List(
                    new StatementSyntax[] { assignIterToVar, filterIfStmnt }
                )
            );

            return CreateForEach(iteratorSupportName, EXPR.Visit(context.expr())(resolver), body);
        };
    }

    public StatementSyntax CreateForEach(IdentifierNameSyntax iteratorName, ExpressionSyntax provider,
        StatementSyntax body)
    {
        var collectionContainerVarRef = GenerateSupportVariable();

        var stepperVarRef = GenerateSupportVariable();

        var collectionContainerInitializer = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("dynamic"),
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.VariableDeclarator(collectionContainerVarRef.Identifier)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(provider))
                })
            )
        );

        var collectionStepInitializer = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("dynamic"),
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.VariableDeclarator(stepperVarRef.Identifier)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(1))))
                })
            )
        );

        var drivingCondition = ExpressionVisitor.CreateBin(
            DmlBinary.GreaterThan,
            stepperVarRef,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                collectionContainerVarRef,
                SyntaxFactory.IdentifierName("len")
            )
        );

        var abortExecutor = SyntaxFactory.IfStatement(
            drivingCondition,
            SyntaxFactory.BreakStatement()
        );

        //Container will always be a list, so use IndexOperator implementation.
        var iteratorAssigner = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("dynamic"),
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.VariableDeclarator(iteratorName.Identifier)
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                ExpressionVisitor.CreateBin(DmlBinary.ArrayIndex, collectionContainerVarRef,
                                    stepperVarRef)
                            )
                        )
                })
            )
        );

        var stepIncrementor =
            SyntaxFactory.ExpressionStatement(
                ExpressionVisitor.CreateUnAsn("++", stepperVarRef, stepperVarRef)
            );

        var drivingLoop = SyntaxFactory.WhileStatement(
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block(
                SyntaxFactory.List(new[]
                {
                    abortExecutor,
                    iteratorAssigner,
                    stepIncrementor,
                    body
                })
            )
        );

        return SyntaxFactory.Block(
            SyntaxFactory.List(new StatementSyntax[]
            {
                collectionContainerInitializer,
                collectionStepInitializer,
                drivingLoop
            })
        );
    }

    public override CodePieceBuilder VisitForlist_list_in([NotNull] DmlParser.Forlist_list_inContext context)
    {
        var path = context.path.GetText();
        var identifierName = Util.IdentifierName(DmlPath.ResolveBaseName(path));
        var baseObj = DmlPath.ResolveParentPath(path);

        return resolver =>
        {
            var objTypeRef = ExpressionVisitor.CreateResolveType(baseObj);

            var enumAtomicTypes = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.EnumerateInstancesOf"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(objTypeRef)
                    })
                )
            );

            return CreateForEach(identifierName, enumAtomicTypes, Visit(context.suite())(resolver));
        };
    }

    public override CodePieceBuilder VisitForlist_decl_in([NotNull] DmlParser.Forlist_decl_inContext context)
    {
        var declDetails = ParseVariableDeclaration(context.variable_declaration());
        return resolver =>
        {
            var body = Visit(context.suite())(resolver);

            if (declDetails.objectType != null)
            {
                //Add a istype filter...
                var filterIfStmnt = SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        EXPR.CreateImplicitIsType(
                            Util.IdentifierName(declDetails.variableName),
                            ExpressionVisitor.CreateResolveType(declDetails.objectType)
                        )
                    ),
                    SyntaxFactory.ContinueStatement(),
                    SyntaxFactory.ElseClause(body)
                );

                body = SyntaxFactory.Block(filterIfStmnt);
            }

            return CreateForEach(Util.IdentifierName(declDetails.variableName), EXPR.Visit(context.expr())(resolver),
                body);
        };
    }

    public override CodePieceBuilder VisitSuite_single_stmt(DmlParser.Suite_single_stmtContext c)
    {
        return resolver =>
        {
            var r = Visit(c.simple_stmt())(resolver);

            var addr = mapping.Lookup(c.start.Line);

            if (addr.HasValue)
            {
                r = r.WithAdditionalAnnotations(
                    BuilderAnnotations.MapSourceFile(addr.Value.FileName, addr.Value.Line));
            }

            return SyntaxFactory.Block(new[]
            {
                r
            });
        };
    }

    public override CodePieceBuilder VisitSuite_multi_stmt(DmlParser.Suite_multi_stmtContext c)
    {
        return resolver => SyntaxFactory.Block(
            c.stmt()
                .Select(s => Tuple.Create(s, Visit(s)))
                .Where(x => x.Item2 != null)
                .Select(s =>
                    {
                        var r = s.Item2(resolver);

                        var addr = mapping.Lookup(s.Item1.Start.Line);

                        if (addr.HasValue)
                        {
                            r = r.WithAdditionalAnnotations(
                                BuilderAnnotations.MapSourceFile(addr.Value.FileName, addr.Value.Line));
                        }

                        return r;
                    }
                ));
    }

    public override CodePieceBuilder VisitSuite_empty([NotNull] DmlParser.Suite_emptyContext context)
    {
        return resolver => SyntaxFactory.Block();
    }

    public override CodePieceBuilder VisitExpr_complex(DmlParser.Expr_complexContext c)
    {
        return resolver => SyntaxFactory.ExpressionStatement(EXPR.Visit(c.children.Single())(resolver));
    }

    public override CodePieceBuilder VisitExpr_bit_binary([NotNull] DmlParser.Expr_bit_binaryContext c)
    {
        return resolver => SyntaxFactory.ExpressionStatement(EXPR.Visit(c)(resolver));
    }

    public override CodePieceBuilder VisitSimple_stmt(DmlParser.Simple_stmtContext context)
    {
        return Visit(context.small_stmt());
    }

    public override CodePieceBuilder VisitReturn_stmt(DmlParser.Return_stmtContext context)
    {
        if (context.ret == null)
            return resolver => SyntaxFactory.ReturnStatement();

        return resolver => SyntaxFactory.ReturnStatement(EXPR.Visit(context.ret)(resolver));
    }

    public override CodePieceBuilder VisitPrereturn_assignment([NotNull] DmlParser.Prereturn_assignmentContext context)
    {
        return resolver =>
            SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName("this.SetImplicitReturn"),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(EXPR.Visit(context.expr())(resolver))
                            }) //.Select(SyntaxFactory.Argument))
                        )
                    )
                )
                .WithAdditionalAnnotations(BuilderAnnotations.ImplicitReturnAssignment);
    }


    public override CodePieceBuilder VisitNew_call_implicit([NotNull] DmlParser.New_call_implicitContext c)
    {
        var rawArgs = c.argument_list() == null
            ? new List<ArgumentPieceBuilder>()
            : EXPR.ParseArgumentList(c.argument_list()).ToList();

        return resolver =>
        {
            var newExpr = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName("ctx.NewAtomic"),
                    SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                rawArgs.Select(y => y(resolver))
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(1))
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.CreateInferNewType(c.dest.GetText()));

            var asn = EXPR.CreateAssignment(
                Util.IdentifierName(c.dest.GetText()),
                newExpr
            );

            return SyntaxFactory.ExpressionStatement(asn);
        };
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Implicit_typed_variable_declarationContext ctx
    )
    {
        return ParseVariableDeclaration(ctx.object_type, ctx.primitive_type, ctx.name, ctx.array, ctx.assignment);
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Implicit_untyped_variable_declarationContext ctx
    )
    {
        return ParseVariableDeclaration(null, ctx.primitive_type, ctx.name, ctx.array, ctx.assignment);
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Variable_declarationContext ctx
    )
    {
        if (ctx.implicit_typed_variable_declaration() != null)
            return ParseVariableDeclaration(ctx.implicit_typed_variable_declaration());

        return ParseVariableDeclaration(ctx.implicit_untyped_variable_declaration());
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Reference_object_tree_pathContext? objectType,
        DmlParser.Identifier_nameContext primitiveType,
        DmlParser.Identifier_nameContext name,
        DmlParser.Array_decl_listContext? array,
        DmlParser.ExprContext? assignment
    )
    {
        TypeSyntax typeDesc;

        //It is ambiguous here if the type is a modifier or type name. So we extract them accordingly.
        string objType = null;
        string variableName = name.GetText();

        bool isListType = array != null;
        var isImplicitInstantiatedList = false;

        if (isListType)
        {
            isImplicitInstantiatedList = array.array_decl().Any(a => a.sz != null);
            objType = "/list";
        }
        else if (objectType != null)
        {
            var modifiers = DmlPath.ExtractTailModifiers(DmlPath.Concat(objectType.GetText(), name.GetText()),
                out var remainder, false);
            objType = remainder.Length > 0 ? DmlPath.RootClassName(remainder) : null;
            variableName = DmlPath.NameWithModifiers(modifiers, name.GetText());
        }

        typeDesc = BuiltinTypes.ResolveGenericType();

        EXPR.PushTypeHint(objType);
        var init = assignment == null ? null : EXPR.Visit(assignment);
        EXPR.PopTypeHint();

        if (init == null && isImplicitInstantiatedList)
            init = resolver =>
            {
                var list_sz_args = array.array_decl().Select(
                    x =>
                        x.sz == null
                            ? SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(0))
                            : EXPR.Visit(x.sz)(resolver)
                ).ToList();

                return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName("ctx.NewAtomic"),
                        SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    list_sz_args.Select(y => SyntaxFactory.Argument(y))
                                        .Prepend(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("/list"))))
                                )
                            )
                            .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(1))
                    )
                    .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke);
            };

        if (init == null)
            init = resolver => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        ExpressionPieceBuilder wrapper = resolver => EXPR.CreateVariable(init(resolver));

        return new VariableDeclarationMetadata
            { variableName = variableName, objectType = objType, init = wrapper, varType = typeDesc };
    }

    public CodePieceBuilder CreateCodePieceBuilder(VariableDeclarationMetadata metaData)
    {
        return resolver =>
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    metaData.varType,
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(metaData.variableName),
                                null,
                                SyntaxFactory.EqualsValueClause(metaData.init(resolver))
                            )
                            .WithAdditionalAnnotations(
                                BuilderAnnotations.CreateTypeHints(metaData.objectType == null
                                    ? ""
                                    : metaData.objectType))
                    })
                )
            );
    }

    public override CodePieceBuilder VisitImplicit_typed_variable_declaration(
        [NotNull] DmlParser.Implicit_typed_variable_declarationContext context)
    {
        var r = ParseVariableDeclaration(context);

        return CreateCodePieceBuilder(r);
    }

    public override CodePieceBuilder VisitImplicit_untyped_variable_declaration(
        [NotNull] DmlParser.Implicit_untyped_variable_declarationContext context)
    {
        var r = ParseVariableDeclaration(context);

        return CreateCodePieceBuilder(r);
    }

    public override CodePieceBuilder VisitDel_statement([NotNull] DmlParser.Del_statementContext context)
    {
        return resolver => SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.Destroy"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(EXPR.Visit(context.expr())(resolver))
                    })
                )
            )
        );
    }

    private ExpressionPieceBuilder ProcessSwitchExpr(IdentifierNameSyntax exprVar, DmlParser.ExprContext r)
    {
        return
            ExpressionVisitor.CreateBin(
                DmlBinary.Equals,
                b => exprVar,
                EXPR.Visit(r)
            );
    }

    private ExpressionPieceBuilder ProcessSwitchSet(IdentifierNameSyntax exprVar, DmlParser.Switch_numsetContext r)
    {
        ExpressionSyntax NumLiteral(int w)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(w));
        }

        var nums = r.NUMBER().Select(p => NumLiteral(int.Parse(p.GetText())));

        var exprs = nums.Select(n =>
            ExpressionVisitor.CreateBin(
                DmlBinary.Equals,
                b => exprVar,
                b => n
            )
        ).ToList();

        var logicalOr = exprs.Aggregate((a, b) => ExpressionVisitor.CreateBin(
                DmlBinary.LogicalOr,
                a,
                b
            )
        );

        return logicalOr;
    }

    private ExpressionPieceBuilder ProcessSwitchRange(IdentifierNameSyntax exprVar, DmlParser.Switch_rangeContext r)
    {
        var fromNum = int.Parse(r.from_range.Text);
        var toNum = int.Parse(r.to_range.Text);

        var lower = Math.Min(fromNum, toNum);
        var upper = Math.Max(fromNum, toNum);

        ExpressionSyntax NumLiteral(int w)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(w));
        }

        return ExpressionVisitor.CreateBin(
            DmlBinary.LogicalAnd,
            ExpressionVisitor.CreateBin(
                DmlBinary.GreaterThanEq,
                b => exprVar,
                b => NumLiteral(lower)
            ),
            ExpressionVisitor.CreateBin(
                DmlBinary.LessThanEq,
                b => exprVar,
                b => NumLiteral(upper)
            )
        );
    }

    private Tuple<ExpressionPieceBuilder, CodePieceBuilder> ProcessSwitchCase(IdentifierNameSyntax exprVar,
        DmlParser.Switch_caseContext r)
    {
        var suite = Visit(r.suite());

        var constraint = r.switch_constraint();
        if (constraint.expr() != null)
            return Tuple.Create(ProcessSwitchExpr(exprVar, constraint.expr()), suite);
        if (constraint.switch_range() != null)
            return Tuple.Create(ProcessSwitchRange(exprVar, constraint.switch_range()), suite);
        if (constraint.switch_numset() != null)
            return Tuple.Create(ProcessSwitchSet(exprVar, constraint.switch_numset()), suite);
        throw new Exception("Unknown switch semantics.");
    }


    public override CodePieceBuilder VisitSwitch_stmnt([NotNull] DmlParser.Switch_stmntContext context)
    {
        var exprEvalPlaceholder = GenerateSupportVariable();

        return r =>
        {
            var initPlaceholder = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName("dynamic"),
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.VariableDeclarator(exprEvalPlaceholder.Identifier)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(EXPR.Visit(context.expr())(r)))
                    })
                )
            );

            var statementExprs = context.switch_case().Select(c => ProcessSwitchCase(exprEvalPlaceholder, c)).ToList();

            return SyntaxFactory.Block(initPlaceholder,
                GenerateSwitchIfStatement(statementExprs,
                    context.else_suite == null ? null : Visit(context.else_suite))(r));
        };
    }

    private CodePieceBuilder GenerateSwitchIfStatement(List<Tuple<ExpressionPieceBuilder, CodePieceBuilder>> exprList,
        CodePieceBuilder? elseClause = null)
    {
        return b =>
        {
            var ifElseIfChain = exprList.Select(i => SyntaxFactory.IfStatement(i.Item1(b), i.Item2(b))).Reverse()
                .ToList();

            if (ifElseIfChain.Count == 0)
                throw new Exception("Cannot have an empty switch statement.");

            // Combine the if-else-if chain into a single if-else-if block
            StatementSyntax ifElseIfBlock;

            if (elseClause != null)
            {
                ifElseIfBlock = elseClause(b);
            }
            else
            {
                ifElseIfBlock = ifElseIfChain.First();
                ifElseIfChain.RemoveAt(0);
            }

            for (var i = 0; i < ifElseIfChain.Count; i++)
                ifElseIfBlock = SyntaxFactory.IfStatement(
                    ifElseIfChain[i].Condition,
                    ifElseIfChain[i].Statement,
                    SyntaxFactory.ElseClause(ifElseIfBlock)
                );

            return ifElseIfBlock;
        };
    }
}