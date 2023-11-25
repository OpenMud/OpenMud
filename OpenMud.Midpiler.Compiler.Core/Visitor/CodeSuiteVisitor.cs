using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.CodeSuiteBuilder;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.TypeSolver;

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
    private string? adjacentLabel = null;
    private bool generatedLabel = false;
    private int vargenIndex;
    private int labelgenIndex = 0;

    private Dictionary<string, string> loopExitLabels = new Dictionary<string, string>();
    private Dictionary<string, string> loopContinueLabels = new Dictionary<string, string>();
    
    private Stack<string> currentLoopExitLabel = new();
    private Stack<string> currentLoopContinueLabel = new();

    internal CodeSuiteVisitor(SourceMapping mapping)
    {
        this.mapping = mapping;
    }

    private string GenerateLabelName(string name)
    {
        return $"lbl_{name}";
    }

    private string GenerateSupportLabel()
    {
        return $"lblsupport_{labelgenIndex++}";
    }

    public IdentifierNameSyntax GenerateSupportVariable()
    {
        return Util.IdentifierName(VARGEN_PREFIX + vargenIndex++);
    }

    public static StatementSyntax GroupStatements(StatementSyntax[] s)
    {
        if (s.Length == 1)
            return s.Single();

        return SyntaxFactory.Block(s);
    }

    public override CodePieceBuilder VisitSpawn_stmt([NotNull] DmlParser.Spawn_stmtContext context)
    {
        return builder =>
        {
            var delayExpression = context.delay == null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))
                : EXPR.Visit(context.delay)(builder)
                ;

            var spawnCall = EXPR.CreateCall("spawn", new[]
            {
                SyntaxFactory.Argument(delayExpression)
            });

            return new StatementSyntax[] {
                SyntaxFactory.IfStatement(
                    spawnCall,
                    SyntaxFactory.Block(
                        SyntaxFactory.List(
                            Visit(context.suite())(builder).Append(
                                SyntaxFactory.ReturnStatement()
                            )
                        )
                    )
                )
            };
        };
    }

    public override CodePieceBuilder VisitIf_stmt(DmlParser.If_stmtContext c)
    {
        var last = c.else_pass == null ? null : Visit(c.else_pass);

        var chains = new List<Tuple<ExpressionPieceBuilder, CodePieceBuilder>>();

        for (var i = 0; i < c.expr().Length; i++)
        {
            var suite = c.suite(i);
            var body = suite == null ? CodePieceBuilderUtil.NullCodePieceBuilder : Visit(suite);
            chains.Add(new Tuple<ExpressionPieceBuilder, CodePieceBuilder>(EXPR.Visit(c.expr(i)), body));
        }

        return resolver =>
        {
            var lastBuilt = last == null ? null : GroupStatements(last(resolver));

            for (var i = chains.Count - 1; i >= 0; i--)
            {
                if (last == null)
                {
                    lastBuilt = SyntaxFactory.IfStatement(
                        ExpressionVisitor.Logical(chains[i].Item1)(resolver),
                        GroupStatements(chains[i].Item2(resolver))
                    );
                }
                else
                {
                    lastBuilt = SyntaxFactory.IfStatement(
                        ExpressionVisitor.Logical(chains[i].Item1)(resolver),
                        GroupStatements(chains[i].Item2(resolver)),
                        SyntaxFactory.ElseClause(lastBuilt)
                    );
                }
            }

            return new[] { lastBuilt };
        };
    }

    private (string exitLabel, string continueLabel, CodePieceBuilder) VisitWrapLoop(DmlParser.SuiteContext ctx, string? adjacentLabel)
    {
        var exitLabel = GenerateSupportLabel();
        var continueLabel = GenerateSupportLabel();

        currentLoopContinueLabel.Push(continueLabel);
        currentLoopExitLabel.Push(exitLabel);

        if (adjacentLabel != null)
        {
            loopContinueLabels[adjacentLabel] = continueLabel;
            loopExitLabels[adjacentLabel] = exitLabel;
        }

        var bodyBuilder = Visit(ctx);

        if (adjacentLabel != null)
        {
            loopContinueLabels.Remove(adjacentLabel);
            loopExitLabels.Remove(adjacentLabel);
        }

        currentLoopContinueLabel.Pop();
        currentLoopExitLabel.Pop();

        return (exitLabel, continueLabel, bodyBuilder);
    }

    public override CodePieceBuilder VisitForlist_list_recycle_in(
        [NotNull] DmlParser.Forlist_list_recycle_inContext context)
    {
        var iterName = Util.IdentifierName(context.iter_var.GetText());

        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);

        return resolver =>
        {
            var iteratorSupportName = GenerateSupportVariable();
            var body = GroupStatements(bodyBulder(resolver));

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

            return new[] {
                CreateForEach(exitLabel, continueLabel, iteratorSupportName, EXPR.Visit(context.expr())(resolver), body)
            };
        };
    }

    public StatementSyntax CreateForEach(string exitLabelName, string continueLabelName, IdentifierNameSyntax iteratorName, ExpressionSyntax provider,
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
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(1)
                            )
                        )
                    )
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

        var continueLabel = CreateLabel(continueLabelName);
        var breakLabel = CreateLabel(exitLabelName);

        var drivingLoop = (StatementSyntax)SyntaxFactory.WhileStatement(
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block(
                SyntaxFactory.List(new[]
                {
                    abortExecutor,
                    iteratorAssigner,
                    stepIncrementor,
                    body,
                    continueLabel
                })
            )
        );

        return SyntaxFactory.Block(
            SyntaxFactory.List(new StatementSyntax[]
            {
                collectionContainerInitializer,
                collectionStepInitializer,
                drivingLoop,
                breakLabel
            })
        );
    }


    public StatementSyntax CreateForLoop(string exitLabelName, string continueLabelName, ExpressionSyntax? condition, StatementSyntax? step, StatementSyntax body)
    {
        var doEvalstepperVarRef = GenerateSupportVariable();

        var doEvalStepperInitializer = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("bool"),
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.VariableDeclarator(doEvalstepperVarRef.Identifier)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.FalseLiteralExpression
                            )
                        )
                    )
                })
            )
        );

        var stepperEvaluator = step == null ? null : SyntaxFactory.IfStatement(
            doEvalstepperVarRef,
            step
        );

        var abortExecutor = condition == null ? null : SyntaxFactory.IfStatement(
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                condition),
            SyntaxFactory.BreakStatement()
        );

        var toggleEvaluator =
            SyntaxFactory.ExpressionStatement(
                ExpressionVisitor.CreateBinAsn("=", doEvalstepperVarRef, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))
            );

        var loopBody = new List<StatementSyntax>();

        if (stepperEvaluator != null)
            loopBody.Add(stepperEvaluator);

        if (abortExecutor != null)
            loopBody.Add(abortExecutor);

        loopBody.Add(toggleEvaluator);
        loopBody.Add(body);

        var continueLabel = CreateLabel(continueLabelName);

        loopBody.Add(continueLabel);

        var drivingLoop = (StatementSyntax)SyntaxFactory.WhileStatement(
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block(
                SyntaxFactory.List(
                    loopBody
                )
            )
        );

        var breakLabel = CreateLabel(exitLabelName);

        return SyntaxFactory.Block(
            SyntaxFactory.List(new StatementSyntax[]
            {
                doEvalStepperInitializer,
                drivingLoop,
                breakLabel
            })
        );
    }

    public override CodePieceBuilder VisitBreak_stmt([NotNull] DmlParser.Break_stmtContext context)
    {
        if (currentLoopExitLabel.Count == 0)
            throw new Exception("Not in a loop!");

        var lbl = currentLoopExitLabel.Peek();

        if(context.NAME() != null)
        {
            if (!loopExitLabels.TryGetValue(context.NAME().GetText(), out lbl))
                throw new Exception("Invalid loop label.");
        }

        return (r) => new[] { CreateGoto(lbl)};
    }

    public override CodePieceBuilder VisitContinue_stmt([NotNull] DmlParser.Continue_stmtContext context)
    {
        if (currentLoopContinueLabel.Count == 0)
            throw new Exception("Not in a loop!");

        var lbl = currentLoopContinueLabel.Peek();


        if (context.NAME() != null)
        {
            if (!loopContinueLabels.TryGetValue(context.NAME().GetText(), out lbl))
                throw new Exception("Invalid loop label.");
        }

        return (r) => new[] { CreateGoto(lbl) };
    }

    public override CodePieceBuilder VisitForlist_list_in([NotNull] DmlParser.Forlist_list_inContext context)
    {
        var path = context.path.GetText();
        var identifierName = Util.IdentifierName(DmlPath.ExtractComponentName(path));
        var baseObj = DmlPath.ResolveParentClass(path);

        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);

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

            return new[] {
                CreateForEach(exitLabel, continueLabel, identifierName, enumAtomicTypes, GroupStatements(bodyBulder(resolver)))
            };
        };
    }

    public override CodePieceBuilder VisitFor_decl([NotNull] DmlParser.For_declContext context)
    {
        var declDetails = ParseVariableDeclaration(context.variable_declaration());
        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);

        return resolver =>
        {
            var body = GroupStatements(bodyBulder(resolver));

            return
                CreateCodePieceBuilder(declDetails)(resolver)
                .Append(
                    CreateForLoop(
                        exitLabel,
                        continueLabel,
                        context.loop_test == null ? null : EXPR.Visit(context.loop_test)(resolver),
                        context.update == null ? null : SyntaxFactory.ExpressionStatement(EXPR.Visit(context.update)(resolver)),
                        body
                    )
                )
                .ToArray();
        };
    }

    public override CodePieceBuilder VisitFor_recycle([NotNull] DmlParser.For_recycleContext context)
    {
        var declDetails = EXPR.Visit(context.initilizer);
        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);
        return resolver =>
        {
            var body = GroupStatements(bodyBulder(resolver));

            return new[] {
                ExpressionVisitor.WrapEnsureStatement(declDetails(resolver)),
                CreateForLoop(
                    exitLabel,
                    continueLabel,
                    context.loop_test == null ? null : EXPR.Visit(context.loop_test)(resolver),
                    context.update == null ? null : SyntaxFactory.ExpressionStatement(EXPR.Visit(context.update)(resolver)),
                    body
                )
            };
        };
    }

    public override CodePieceBuilder VisitFor_nodecl([NotNull] DmlParser.For_nodeclContext context)
    {
        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);
        return resolver =>
        {
            var body = GroupStatements(bodyBulder(resolver));

            return new[]
            {
                CreateForLoop(
                    exitLabel,
                    continueLabel,
                    context.loop_test == null ? null : EXPR.Visit(context.loop_test)(resolver),
                    context.update == null ? null : SyntaxFactory.ExpressionStatement(EXPR.Visit(context.update)(resolver)),
                    body
                )
            };
        };
    }

    public override CodePieceBuilder VisitWhile_stmt([NotNull] DmlParser.While_stmtContext context)
    {
        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);
        return resolver =>
        {
            var body = GroupStatements(bodyBulder(resolver));

            return new[]
            {
                CreateForLoop(
                    exitLabel,
                    continueLabel,
                    EXPR.Visit(context.expr())(resolver),
                    null,
                    body
                )
            };
        };
    }

    public override CodePieceBuilder VisitForlist_decl_in([NotNull] DmlParser.Forlist_decl_inContext context)
    {
        var declDetails = ParseVariableDeclaration(context.variable_declaration());
        var (exitLabel, continueLabel, bodyBulder) = VisitWrapLoop(context.suite(), this.adjacentLabel);
        return resolver =>
        {
            var body = GroupStatements(bodyBulder(resolver));

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

            return new[] {
                CreateForEach(
                    exitLabel,
                    continueLabel,
                    Util.IdentifierName(declDetails.variableName),
                    EXPR.Visit(context.expr())(resolver),
                    body
                )
            };
        };
    }

    public override CodePieceBuilder VisitSuite_single_stmt(DmlParser.Suite_single_stmtContext c)
    {
        var builder = Visit(c.simple_stmt());

        return resolver =>
        {
            var r = builder(resolver).Single();

            var addr = mapping.Lookup(c.start.Line);

            if (addr.HasValue)
            {
                r = r.WithAdditionalAnnotations(
                    BuilderAnnotations.MapSourceFile(addr.Value.FileName, addr.Value.Line));
            }

            return new[] {
                SyntaxFactory.Block(new[]
                {
                    r
                })
            };
        };
    }

    public override CodePieceBuilder VisitSuite_compound_stmt([NotNull] DmlParser.Suite_compound_stmtContext c)
    {
        return resolver =>
        {
            var r = Visit(c.compound_stmt())(resolver).Single();

            var addr = mapping.Lookup(c.start.Line);

            if (addr.HasValue)
            {
                r = r.WithAdditionalAnnotations(
                    BuilderAnnotations.MapSourceFile(addr.Value.FileName, addr.Value.Line));
            }

            return new[] {
                SyntaxFactory.Block(new[]
                {
                    r
                })
            };
        };
    }

    private CodePieceBuilder ParseStatementsAsBlock(IEnumerable<ParserRuleContext> statements) {
        var stmtBuilders = statements
            .Select(s => Tuple.Create(s, Visit(s)))
            .ToList();

        return resolver =>
        {
            var block = SyntaxFactory.Block(
                stmtBuilders
                .Where(x => x.Item2 != null)
                .SelectMany(s =>
                {
                    var r = s.Item2(resolver);

                    var addr = mapping.Lookup(s.Item1.Start.Line);

                    if (addr.HasValue)
                    {
                        r = r.Select(
                            w =>
                                w.WithAdditionalAnnotations(
                                    BuilderAnnotations.MapSourceFile(addr.Value.FileName, addr.Value.Line)
                                )
                            ).ToArray();
                    }

                    return r;
                }
                )
            );

            return new[] { block };
        };
    }

    public override CodePieceBuilder VisitSuite_multi_stmt(DmlParser.Suite_multi_stmtContext c)
    {
        var statements = c.stmt().Cast<ParserRuleContext>().Concat(
            c.stmt_list_item().Select(c => (ParserRuleContext)c.compound_stmt() ?? c.small_stmt())
        ).ToList();
        return ParseStatementsAsBlock(statements);
    }

    public override CodePieceBuilder VisitSuite_empty([NotNull] DmlParser.Suite_emptyContext context)
    {
        return resolver => new[] { SyntaxFactory.Block() };
    }

    public override CodePieceBuilder VisitExpr_complex(DmlParser.Expr_complexContext c)
    {
        return resolver => new[] { SyntaxFactory.ExpressionStatement(EXPR.Visit(c.children.Single())(resolver)) };
    }

    public override CodePieceBuilder VisitExpr_bit_binary([NotNull] DmlParser.Expr_bit_binaryContext c)
    {
        return resolver => new[] { SyntaxFactory.ExpressionStatement(EXPR.Visit(c)(resolver)) };
    }

    public override CodePieceBuilder VisitSimple_stmt(DmlParser.Simple_stmtContext context)
    {
        return Visit(context.small_stmt());
    }

    public override CodePieceBuilder VisitReturn_stmt(DmlParser.Return_stmtContext context)
    {
        if (context.ret == null)
            return resolver => new[] { SyntaxFactory.ReturnStatement() };

        return resolver => new[] { SyntaxFactory.ReturnStatement(EXPR.Visit(context.ret)(resolver)) };
    }

    public override CodePieceBuilder VisitPrereturn_assignment([NotNull] DmlParser.Prereturn_assignmentContext context)
    {
        return resolver =>
        {
            var r = SyntaxFactory.ExpressionStatement(
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

            return new[] { r };
        };
    }

    public override CodePieceBuilder VisitPrereturn_augmentation(DmlParser.Prereturn_augmentationContext c)
    {
        return resolver =>
        {
            var assignment = ExpressionVisitor.CreateBinAsn(
                c.augAsnOp().GetText(), 
                ExpressionVisitor.CreatePrereturnExpression(),
                EXPR.Visit(c.src)(resolver)
            );

            var r = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName("this.SetImplicitReturn"),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(assignment)
                            }) //.Select(SyntaxFactory.Argument))
                        )
                    )
                )
                .WithAdditionalAnnotations(BuilderAnnotations.ImplicitReturnAssignment);

            return new[] { r }; 
        };
    }


    public override CodePieceBuilder VisitNew_call_implicit([NotNull] DmlParser.New_call_implicitContext c)
    {
        var rawArgs = c.argument_list() == null
            ? new List<ArgumentPieceBuilder>()
            : EXPR.ParseArgumentList(c.argument_list()).ToList();

        var fieldInitExpr = c.new_call_field_initializer_list() == null ? null : EXPR.ParseFieldInitExpression(c.new_call_field_initializer_list());

        return resolver =>
        {
            ExpressionSyntax newExpr = SyntaxFactory.InvocationExpression(
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

            if (fieldInitExpr != null)
                newExpr = EXPR.WrapFieldInitializer(newExpr, fieldInitExpr.Select(x => Tuple.Create(x.Item1, x.Item2(resolver))));

            var asn = EXPR.CreateAssignment(
                Util.IdentifierName(c.dest.GetText()),
                newExpr
            );

            return new[] { SyntaxFactory.ExpressionStatement(asn) };
        };
    }


    public override CodePieceBuilder VisitNew_call_indirect([NotNull] DmlParser.New_call_indirectContext c)
    {
        var rawArgs = c.argument_list() == null
            ? new List<ArgumentPieceBuilder>()
            : EXPR.ParseArgumentList(c.argument_list()).ToList();

        var subject = EXPR.Visit(c.dest);
        var field = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(c.field.GetText()));

        var fieldInitExpr = c.new_call_field_initializer_list() == null ? null : EXPR.ParseFieldInitExpression(c.new_call_field_initializer_list());

        return resolver =>
        {
            var newExpr = EXPR.CreateCall(RuntimeFrameworkIntrinsic.INDIRECT_NEW,
                SyntaxFactory.SeparatedList(
                    new[]
                    {
                        SyntaxFactory.Argument(subject(resolver)),
                        SyntaxFactory.Argument(field)
                    }.Concat(
                        rawArgs.Select(y => y(resolver))
                    )
                )
            );


            if (fieldInitExpr != null)
                newExpr = EXPR.WrapFieldInitializer(newExpr, fieldInitExpr.Select(x => Tuple.Create(x.Item1, x.Item2(resolver))));

            return new[] { SyntaxFactory.ExpressionStatement(newExpr) };
        };
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Implicit_typed_variable_declarationContext ctx,
        string? typePrefix = null
    )
    {
        return ParseVariableDeclaration(ctx.object_type, ctx.primitive_type, ctx.name, ctx.array, ctx.assignment, typePrefix);
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Implicit_untyped_variable_declarationContext ctx,
        string? typePrefix = null
    )
    {
        return ParseVariableDeclaration(null, ctx.primitive_type, ctx.name, ctx.array, ctx.assignment, typePrefix);
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Variable_declarationContext ctx,
        string? typePrefix = null
    )
    {
        if (ctx.implicit_typed_variable_declaration() != null)
            return ParseVariableDeclaration(ctx.implicit_typed_variable_declaration(), typePrefix);

        return ParseVariableDeclaration(ctx.implicit_untyped_variable_declaration(), typePrefix);
    }

    public VariableDeclarationMetadata ParseVariableDeclaration(
        DmlParser.Reference_object_tree_pathContext? objectType,
        DmlParser.Identifier_nameContext primitiveType,
        DmlParser.Identifier_nameContext name,
        DmlParser.Array_decl_listContext? array,
        DmlParser.ExprContext? assignment,
        string objectTypePrefix = null
    )
    {
        TypeSyntax typeDesc;

        //It is ambiguous here if the type is a modifier or type name. So we extract them accordingly.
        string? objType = null;
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
            objType = objectType.GetText();
        }

        if(objectTypePrefix != null)
            objType = objectTypePrefix + (objType ?? "");

        if (objType != null)
        {
            var modifiers = DmlPath.ParseDeclarationPath(DmlPath.Concat(objType, name.GetText()), out var remainder, out var _, true);
            objType = remainder.Length > 0 ? DmlPath.BuildQualifiedDeclarationName(remainder) : null;
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


        ExpressionPieceBuilder wrapper = (resolver) =>
        {
            var target = init(resolver);
            var r = EXPR.CreateVariable(target);
            if (target.HasAnnotation(BuilderAnnotations.DmlInvoke))
                r = r.WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke);

            return r;
        };

        return new VariableDeclarationMetadata
            { variableName = variableName, objectType = objType, init = wrapper, varType = typeDesc };
    }

    public CodePieceBuilder CreateCodePieceBuilder(VariableDeclarationMetadata metaData)
    {
        return resolver =>
        {
            var decl = SyntaxFactory.LocalDeclarationStatement(
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

            return new[] { decl };
        };
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

    public override CodePieceBuilder VisitVariable_set_declaration([NotNull] DmlParser.Variable_set_declarationContext context)
    {
        var prefix = context.path_prefix?.GetText();
        var decls = context.varset_suite.implicit_variable_declaration();
        var typedDecl = decls
            .Select(p => p.implicit_typed_variable_declaration())
            .Where(p => p != null)
            .Select(p => ParseVariableDeclaration(p, prefix));

        var untypedDecl = decls
                    .Select(p => p.implicit_untyped_variable_declaration())
                    .Where(p => p != null)
                    .Select(p => ParseVariableDeclaration(p, prefix));

        return b =>
        {
            return typedDecl
                    .Concat(untypedDecl)
                    .Select(CreateCodePieceBuilder)
                    .SelectMany(i => i(b))
                    .ToArray();
        };
    }

    public override CodePieceBuilder VisitDel_statement([NotNull] DmlParser.Del_statementContext context)
    {
        return resolver =>
        {
            var r = SyntaxFactory.ExpressionStatement(
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

            return new[] { r };
        };
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

    private ExpressionPieceBuilder ProcessSwitchSet(IdentifierNameSyntax exprVar, DmlParser.Switch_exprsetContext r)
    {
        var nums = r.expr().Select(EXPR.Visit).ToList();

        return (resolver) =>
        {
            var exprs = nums.Select(n =>
                ExpressionVisitor.CreateBin(
                    DmlBinary.Equals,
                    b => exprVar,
                    b => n(resolver)
                )
            ).ToList();

            var logicalOr = exprs.Aggregate((a, b) => ExpressionVisitor.CreateBin(
                    DmlBinary.LogicalOr,
                    a,
                    b
                )
            );

            return logicalOr(resolver);
        };
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
        if (constraint.switch_exprset() != null)
            return Tuple.Create(ProcessSwitchSet(exprVar, constraint.switch_exprset()), suite);
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

            return new[] {
                SyntaxFactory.Block(
                    initPlaceholder,
                    GroupStatements(GenerateSwitchIfStatement(
                        statementExprs,
                        context.else_suite == null ? null : Visit(context.else_suite)
                    )(r))
                )
            };
        };
    }

    private CodePieceBuilder GenerateSwitchIfStatement(List<Tuple<ExpressionPieceBuilder, CodePieceBuilder>> exprList,
        CodePieceBuilder? elseClause = null)
    {
        return b =>
        {
            var ifElseIfChain = exprList.Select(i => SyntaxFactory.IfStatement(i.Item1(b), GroupStatements(i.Item2(b)))).Reverse()
                .ToList();

            if (ifElseIfChain.Count == 0)
                throw new Exception("Cannot have an empty switch statement.");

            // Combine the if-else-if chain into a single if-else-if block
            StatementSyntax ifElseIfBlock;

            if (elseClause != null)
            {
                ifElseIfBlock = GroupStatements(elseClause(b));
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

            return new[] { ifElseIfBlock };
        };
    }


    private StatementSyntax CreateLabel(string name)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("InlineIL.IL.MarkLabel"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(GenerateLabelName(name))
                            )
                            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation)
                        )
                    })
                )
            )
        );
    }

    private StatementSyntax CreateGoto(string name)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("InlineIL.IL.Emit.Br"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(GenerateLabelName(name))
                            )
                            .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation)
                        )
                    })
                )
            )
        );
    }

    public override CodePieceBuilder VisitGoto_label_declaration([NotNull] DmlParser.Goto_label_declarationContext c)
    {
        //Need to create a closure (block) because it marks a potential re-entry boundary for async operation rewriter builder step
        adjacentLabel = c.NAME().GetText();
        generatedLabel = true;
        var body = ParseStatementsAsBlock(c.stmt());
        return (resolver) =>
        {
            return new[] {
                    SyntaxFactory.Block(new StatementSyntax[] {
                        CreateLabel(c.NAME().GetText())
                    }
                    .Concat(body(resolver))
                )
            };
        };
    }

    public override CodePieceBuilder VisitGoto_stmt([NotNull] DmlParser.Goto_stmtContext context)
    {
        return resolver => new[] { CreateGoto(context.NAME().GetText()) };
    }

    public override CodePieceBuilder VisitStmt([NotNull] DmlParser.StmtContext context)
    {
        var r = base.VisitStmt(context);
        if (!generatedLabel)
            this.adjacentLabel = null;

        this.generatedLabel = false;
        return r;
    }

    public override CodePieceBuilder VisitStmt_list([NotNull] DmlParser.Stmt_listContext context)
    {

        var builders = context.stmt_list_item()
            .Select(x => (IParseTree)x.compound_stmt() ?? x.small_stmt())
            .Select(Visit)
            .Where(x => x != null)
            .ToList();

        return (resolver) =>
            builders.SelectMany(r => r(resolver)).ToArray();
    }

}