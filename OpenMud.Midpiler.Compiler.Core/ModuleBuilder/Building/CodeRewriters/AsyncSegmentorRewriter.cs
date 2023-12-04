using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class ReplaceTopLevelInvoction : CSharpSyntaxRewriter
{
    private readonly Dictionary<BlockSyntax, List<Tuple<InvocationExpressionSyntax, BlockSyntax>>> blockGroupings;
    private readonly Dictionary<InvocationExpressionSyntax, string> topLevelAnchorNames;

    public ReplaceTopLevelInvoction(
        Dictionary<BlockSyntax, List<Tuple<InvocationExpressionSyntax, BlockSyntax>>> blockGroupings,
        Dictionary<InvocationExpressionSyntax, string> topLevelAnchorNames)
    {
        this.blockGroupings = blockGroupings;
        this.topLevelAnchorNames = topLevelAnchorNames;
    }


    private (ExpressionSyntax deferred, bool mustDefer) CreateDeferredEvalInitExpression(
        InvocationExpressionSyntax invoke)
    {
        var immediateDeps = invoke
            .ArgumentList
            .DescendantNodes(n => !(n is InvocationExpressionSyntax))
            .Where(n => n is InvocationExpressionSyntax)
            .Cast<InvocationExpressionSyntax>()
            .Select(n => (
                    originExpr: n,
                    deferral: CreateDeferredEvalInitExpression(n)
                )
            )
            .ToList();

        var dependence = immediateDeps
            .Where(n => n.deferral.mustDefer)
            .Select((n, i) => (n, i))
            .ToDictionary(
                n => n.n,
                n => n.i
            );

        var dependenceRawArgs = dependence.Select(n => n.Key.deferral.deferred).ToList();

        invoke = invoke.ReplaceNodes(dependence.Select(n => n.Key.originExpr), (raw, _) =>
        {
            var idx = dependence.Where(x => x.Key.originExpr == raw).First().Value;

            var el = SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.IdentifierName("deps"),
                SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(idx))
                        )
                    })
                )
            );

            return SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("dynamic"), el));
        });

        if (!invoke.HasAnnotation(BuilderAnnotations.DmlInvoke))
            return (invoke, false);

        var depsArgumentType =
            SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(typeof(DmlDeferredEvaluation).FullName),
                SyntaxFactory.List(new[] { SyntaxFactory.ArrayRankSpecifier() }));

        var depsArgument =
            SyntaxFactory.ArrayCreationExpression(
                depsArgumentType,
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                    SyntaxFactory.SeparatedList(dependenceRawArgs)
                )
            );


        var refTypeParameterList =
            SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(typeof(EnvObjectReference).FullName),
                SyntaxFactory.List(new[] { SyntaxFactory.ArrayRankSpecifier() }));
        var ctxBuilder =
            SyntaxFactory.AnonymousMethodExpression(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("deps")).WithType(refTypeParameterList)
                    })
                ),
                SyntaxFactory.Block(
                    SyntaxFactory.SeparatedList<StatementSyntax>(new[]
                    {
                        SyntaxFactory.ReturnStatement(
                            invoke
                        )
                    })
                )
            );

        var deferHandler = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseName("CreateDeferred"),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(depsArgument),
                    SyntaxFactory.Argument(ctxBuilder)
                })
            )
        );

        return (deferHandler, true);
    }

    private SyntaxNode CreateBlockWithInvokeInitializers(BlockSyntax block,
        List<Tuple<InvocationExpressionSyntax, BlockSyntax>> list)
    {
        var initTargets = list.ToDictionary(
            x => topLevelAnchorNames[x.Item1],
            x => CreateDeferredEvalInitExpression(x.Item1));

        var initStatements = initTargets.Select(t =>
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(t.Key),
                    t.Value.deferred
                )
            );
        });

        return block.WithStatements(SyntaxFactory.List(
            initStatements.Concat(
                block.Statements
            )
        ));
    }

    private SyntaxNode CreateExecuteDeferred(string deferredObjectName)
    {
        return
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(deferredObjectName),
                    SyntaxFactory.IdentifierName("ExecuteDynamic")
                ),
                SyntaxFactory.ArgumentList()
            );
    }

    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        var inner = (BlockSyntax)base.VisitBlock(node);

        if (blockGroupings.ContainsKey(node))
            inner = (BlockSyntax)CreateBlockWithInvokeInitializers(inner, blockGroupings[node]);

        return inner;
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (topLevelAnchorNames.ContainsKey(node))
            return CreateExecuteDeferred(
                topLevelAnchorNames[(InvocationExpressionSyntax)base.VisitInvocationExpression(node)]);

        return base.VisitInvocationExpression(node);
    }
}

internal class InjectStepSuccession : CSharpSyntaxRewriter
{
    private int currentStep = 1;

    public int InjectedSteps => currentStep - 1;

    public readonly List<Tuple<Tuple<int, int>, int>> ExceptionHandlers = new();

    private ExpressionStatementSyntax CreateStepSuccession(out int stepNo)
    {
        stepNo = currentStep++;
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName("currentStep")
                ),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(stepNo)
                )
            ));
    }

    private ExpressionStatementSyntax CreateStepLabel(int stepNo)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ParseExpression($"InlineIL.IL.MarkLabel(\"exec_{stepNo}\")"));
    }

    public BlockSyntax WrapBlock(StatementSyntax s)
    {
        var advance = CreateStepSuccession(out var stepNo);

        var label = CreateStepLabel(stepNo);

        return SyntaxFactory.Block(SyntaxFactory.List(new[]
        {
            advance,
            label,
            s
        }));
    }

    public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
    {
        return node;
    }

    public override SyntaxNode? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
    {
        return node;
    }

    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        var inner = (StatementSyntax)base.VisitBlock(node);
        return WrapBlock(inner);
    }

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        var inner = (StatementSyntax)base.VisitExpressionStatement(node);
        return WrapBlock(inner);
    }

    public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
    {
        return WrapBlock
        (node.WithStatement(
                WrapBlock(
                    (StatementSyntax)base.Visit(node.Statement)
                )
            )
        );
    }

    public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
    {
        return node.WithStatement(
            WrapBlock(
                (StatementSyntax)base.Visit(node.Statement)
            )
        );
    }

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        return WrapBlock(
            node.WithStatement(
                (StatementSyntax)base.Visit(node.Statement)
            )
        );
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        return
            WrapBlock(
                node.WithStatement(
                    WrapBlock(
                        (StatementSyntax)base.Visit(node.Statement)
                    )
                )
            );
    }

    public override SyntaxNode? VisitTryStatement(TryStatementSyntax node)
    {
        
        var exceptionHandlerBegin = currentStep;
        StatementSyntax body = (StatementSyntax)Visit(node.Block);
        var exceptionHandlerEnd = currentStep - 1;
        var catchBegin = currentStep;
        StatementSyntax catchBody = (StatementSyntax)Visit(node.Catches.Single().Block);
        var catchEndAsn = CreateStepSuccession(out var catchEnd);
        var catchEndLabel = CreateStepLabel(catchEnd);
        var skipCatch = AsyncSegmentorRewriter.GenerateGotoSequencePoint(catchEnd);

        ExceptionHandlers.Add(Tuple.Create(Tuple.Create(exceptionHandlerBegin, exceptionHandlerEnd), catchBegin));

        return SyntaxFactory.Block(new[] {
            body,
            skipCatch,
            catchBody,
            catchEndAsn,
            catchEndLabel
        });
    }
}

public class AsyncSegmentorRewriter : CSharpSyntaxRewriter
{
    private Dictionary<BlockSyntax, List<Tuple<InvocationExpressionSyntax, BlockSyntax>>>? blockGroupings;
    private readonly List<Tuple<InvocationExpressionSyntax, BlockSyntax>> injectionPoints = new();
    private bool injectJumps;
    private Dictionary<InvocationExpressionSyntax, string>? topLevelAnchorNames;
    private List<Tuple<Tuple<int, int>, int>>? LastExceptionHandlers = null;
    private BlockSyntax FindBlock(SyntaxNode node)
    {
        SyntaxNode? n;
        for (n = node; !(n is BlockSyntax) && n.Parent != null; n = n.Parent) ;

        if (n == null)
            throw new Exception("Does not exist in a block.");

        return (BlockSyntax)n;
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (injectJumps && node.HasAnnotation(BuilderAnnotations.DmlInvoke))
        {
            injectionPoints.Add(Tuple.Create(node, FindBlock(node)));
            return node;
        }

        return base.VisitInvocationExpression(node);
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        LastExceptionHandlers = null;

        node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

        node = node.WithMembers(SyntaxFactory.List(
            node.Members.Concat(
                GetAnchorDeclarations()
            )
        ));

        if (topLevelAnchorNames != null && topLevelAnchorNames.Any())
        {
            var currentCloneGenerator = (MethodDeclarationSyntax)node
                .Members
                .Where(m => m is MethodDeclarationSyntax method && method.Identifier.Text == "DmlGenerateClone")
                .Single();

            node = node.ReplaceNode(currentCloneGenerator, GenerateCloneConstructor(currentCloneGenerator));
        }

        topLevelAnchorNames = null;
        blockGroupings = null;
        injectionPoints.Clear();

        if(LastExceptionHandlers != null)
            node = DmlMethodBuilder.BuildExceptionHandlers(node, LastExceptionHandlers);

        return node;
    }

    private SyntaxNode GenerateCloneConstructor(MethodDeclarationSyntax currentCloneGenerator)
    {
        var cloneBody = currentCloneGenerator.Body.Statements.ToList();
        var returnStatement = cloneBody.Last();

        cloneBody.Remove(returnStatement);

        var memberAssignmentExpressions = topLevelAnchorNames.Select(a => a.Value).Select(d =>
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
                            ),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("clonedCtx"))
                                })
                            )
                        )
                    )
                )
            )
        );

        cloneBody.AddRange(memberAssignmentExpressions);
        cloneBody.Add(returnStatement);

        return currentCloneGenerator.WithBody(
            currentCloneGenerator.Body.WithStatements(
                SyntaxFactory.List(cloneBody)
            )
        );
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!BuilderAnnotations.HasProcNameAnnotation(node))
            return base.VisitMethodDeclaration(node);

        injectJumps = true;
        injectionPoints.Clear();

        node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

        topLevelAnchorNames = GetAnchorNames();

        blockGroupings = injectionPoints.GroupBy(n => n.Item2).ToDictionary(x => x.Key, x => x.ToList());

        node = (MethodDeclarationSyntax)new ReplaceTopLevelInvoction(blockGroupings, topLevelAnchorNames).Visit(node);

        var successionGenerator = new InjectStepSuccession();

        node = (MethodDeclarationSyntax)successionGenerator.Visit(node);
        node = InjectEntrypointDispatcher(node, successionGenerator.InjectedSteps);

        LastExceptionHandlers = successionGenerator.ExceptionHandlers.Any() ? successionGenerator.ExceptionHandlers.ToList() : null;

        return node;
    }

    public static StatementSyntax GenerateGotoSequencePoint(int sequenceId)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression($"InlineIL.IL.Emit.Br(\"exec_{sequenceId}\")"));
    }

    private MethodDeclarationSyntax? InjectEntrypointDispatcher(MethodDeclarationSyntax? node, int injectedSteps)
    {
        var currentStepExpr = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ThisExpression(),
            SyntaxFactory.IdentifierName("currentStep")
        );
        var switchStatement = SyntaxFactory.SwitchStatement(
            currentStepExpr,
            SyntaxFactory.List(
                Enumerable.Range(0, injectedSteps).Select(s =>
                        SyntaxFactory.SwitchSection()
                            .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                                SyntaxFactory.CaseSwitchLabel(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(s + 1)))))
                            .WithStatements(SyntaxFactory.List(new StatementSyntax[]
                            {
                                GenerateGotoSequencePoint(s + 1),
                                SyntaxFactory.BreakStatement()
                            }))
                    )
                    .Concat(
                        new[]
                        {
                            SyntaxFactory.SwitchSection()
                                .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                                    SyntaxFactory.CaseSwitchLabel(
                                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(0)))))
                                .WithStatements(
                                    SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.BreakStatement())),
                            SyntaxFactory.SwitchSection()
                                .WithLabels(
                                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()))
                                .WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ThrowStatement(SyntaxFactory
                                        .ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.Exception"))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList(
                                                    new[]
                                                    {
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.StringLiteralExpression,
                                                                SyntaxFactory.Literal(
                                                                    "Proc dispatcher did not identify the current step.")))
                                                    }
                                                )
                                            )))))
                        }
                    )
            )
        );
        ;

        return node.WithBody(
            node.Body.WithStatements(
                SyntaxFactory.List(
                    node.Body.Statements
                        .Prepend(switchStatement)
                )
            )
        );
    }

    private Dictionary<InvocationExpressionSyntax, string> GetAnchorNames()
    {
        var names = injectionPoints.Select((x, i) => (x, i)).ToDictionary(
            x => x.x.Item1,
            x => "invoke_anchor_" + x.i);

        return names;
    }

    private MemberDeclarationSyntax[] GetAnchorDeclarations()
    {
        var names = GetAnchorNames();

        var declarations = names.Values.Select(n =>
            {
                MemberDeclarationSyntax d = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName(typeof(DmlDeferredEvaluation).FullName),
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.VariableDeclarator(n)
                        })
                    )
                );
                return d;
            })
            .ToArray();

        return declarations;
    }
}