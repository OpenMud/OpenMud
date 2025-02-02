using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.GrammarSupport;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.CodeSuiteBuilder;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using static OpenMud.Mudpiler.Compiler.DmlGrammar.DmlParser;

namespace OpenMud.Mudpiler.Compiler.Core.Visitor;

public class ExpressionVisitor : DmlParserBaseVisitor<ExpressionPieceBuilder>
{
    private readonly Stack<string> typeHintCtx = new();

    public static ExpressionPieceBuilder Logical(ExpressionPieceBuilder v)
    {
        return CreateUn(DmlUnary.Logical, v);
    }

    public static ArgumentSyntax CreateAssignmentDelegate(ExpressionSyntax subject)
    {
        return SyntaxFactory.Argument(
            SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName("System.Action<dynamic>"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.ParenthesizedLambdaExpression(
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SeparatedList(
                                        new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier("___p")
                                            )
                                        }
                                    )
                                ),
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    subject,
                                    SyntaxFactory.ParseExpression("___p")
                                )
                            )
                        )
                    })
                ),
                null
            )
        );
    }


    public static ArgumentSyntax CreateBlankAssignmentDelegate()
    {
        return SyntaxFactory.Argument(
            SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName("System.Action<dynamic>"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.ParenthesizedLambdaExpression(
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SeparatedList(
                                        new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier("___p")
                                            )
                                        }
                                    )
                                ),
                                SyntaxFactory.Block()
                            )
                        )
                    })
                ),
                null
            )
        );
    }

    private static ExpressionPieceBuilder CreateBin(string op, ExpressionPieceBuilder left,
        ExpressionPieceBuilder right)
    {
        return CreateBin(DmlOperation.ParseBinary(op), left, right);
    }


    public static ExpressionSyntax CreateBin(DmlBinary op, ExpressionSyntax left, ExpressionSyntax right)
    {
        if (DmlOperation.IsOperandsReversed(op))
        {
            ExpressionSyntax b;
            b = left;
            left = right;
            right = b;
        }

        var dmlOp = typeof(DmlBinary).FullName + "." + Enum.GetName(typeof(DmlBinary), op);
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.op.Binary"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.ParseExpression(dmlOp)),
                        SyntaxFactory.Argument(left),
                        SyntaxFactory.Argument(right)
                    }) //.Select(SyntaxFactory.Argument))
                )
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
            .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }


    public static ExpressionPieceBuilder CreateBin(DmlBinary op, ExpressionPieceBuilder left,
        ExpressionPieceBuilder right)
    {
        return resolver => CreateBin(op, left(resolver), right(resolver));
    }

    private static ExpressionPieceBuilder CreateTernery(DmlTernery op, ExpressionPieceBuilder left,
        ExpressionPieceBuilder mid, ExpressionPieceBuilder right)
    {
        var dmlOp = typeof(DmlTernery).FullName + "." + Enum.GetName(typeof(DmlTernery), op);
        return resolver =>
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName("ctx.op.Ternery"),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression(dmlOp)),
                            SyntaxFactory.Argument(left(resolver)),
                            SyntaxFactory.Argument(mid(resolver)),
                            SyntaxFactory.Argument(right(resolver))
                        }) //.Select(SyntaxFactory.Argument))
                    )
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }

    public ExpressionSyntax CreateAssignment(ExpressionSyntax left, ExpressionSyntax right)
    {
        var dmlOp = typeof(DmlBinaryAssignment).FullName + "." +
                    Enum.GetName(typeof(DmlBinaryAssignment), DmlBinaryAssignment.Assignment);
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.op.BinaryAssignment"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.ParseExpression(dmlOp)),
                        SyntaxFactory.Argument(left),
                        SyntaxFactory.Argument(right)
                        //blankAssignment ? CreateBlankAssignmentDelegate() : CreateAssignmentDelegate(left(resolver))//SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), left(resolver))
                    }) //.Select(SyntaxFactory.Argument))
                )
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
            .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }

    public ExpressionSyntax CreateImmediateEval(ExpressionSyntax interim)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                interim,
                SyntaxFactory.IdentifierName("CompleteOrException")
            ),
            SyntaxFactory.ArgumentList()
        );
    }

    public ExpressionSyntax CreateVariable(ExpressionSyntax init = null)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(
                    typeof(VarEnvObjectReference).FullName
                ),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName("Variable")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    init == null
                        ? null
                        : new[]
                        {
                            SyntaxFactory.Argument(init)
                        }
                )
            )
        );
    }

    public static ExpressionSyntax CreateBinAsn(string op, ExpressionSyntax left,
        ExpressionSyntax right)
    {
        var dmlOp = typeof(DmlBinaryAssignment).FullName + "." +
                    Enum.GetName(typeof(DmlBinaryAssignment), DmlOperation.ParseBinaryAsn(op));

        return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName("ctx.op.BinaryAssignment"),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression(dmlOp)),
                            SyntaxFactory.Argument(left),
                            SyntaxFactory.Argument(right)
                            //blankAssignment ? CreateBlankAssignmentDelegate() : CreateAssignmentDelegate(left(resolver))//SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), left(resolver))
                        }) //.Select(SyntaxFactory.Argument))
                    )
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }

    private static ExpressionPieceBuilder CreateBinAsn(string op, ExpressionPieceBuilder left,
        ExpressionPieceBuilder right)
    {
        return resolver => CreateBinAsn(op, left(resolver), right(resolver));
    }


    private static ExpressionPieceBuilder CreateUn(string op, ExpressionPieceBuilder subject)
    {
        return CreateUn(DmlOperation.ParseUnary(op), subject);
    }

    public static ExpressionSyntax CreateUn(DmlUnary op, ExpressionSyntax subject)
    {
        var dmlOp = typeof(DmlUnary).FullName + "." + Enum.GetName(typeof(DmlUnary), op);
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.op.Unary"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.ParseExpression(dmlOp)),
                        SyntaxFactory.Argument(subject)
                    }) //.Select(SyntaxFactory.Argument))
                )
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
            .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }


    private static ExpressionPieceBuilder CreateUn(DmlUnary op, ExpressionPieceBuilder subject)
    {
        return resolver => CreateUn(op, subject(resolver));
    }

    public ExpressionSyntax CreatePrimitiveAssertType(ExpressionSyntax subject, string[] primitivetypeNames)
    {
        var typeNames = primitivetypeNames.Select(t =>
            (ExpressionSyntax)SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(t))
        ).ToList();

        return CreateCall("assert_primitive", new[] {
            SyntaxFactory.Argument(subject),
            SyntaxFactory.Argument(CreateListLiteral(typeNames))
        });
    }

    public static ExpressionSyntax CreateUnAsn(string op, ExpressionSyntax subject, ExpressionSyntax source,
        bool isPreOp = true)
    {
        var dmlOp = typeof(DmlUnaryAssignment).FullName + "." +
                    Enum.GetName(typeof(DmlUnaryAssignment), DmlOperation.ParseUnaryAsn(op, isPreOp));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.op.UnaryAssignment"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.ParseExpression(dmlOp)),
                        SyntaxFactory
                            .Argument(
                                source)
                    })
                )
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
            .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }

    private static ExpressionPieceBuilder CreateUnAsn(string op, ExpressionPieceBuilder subject,
        ExpressionPieceBuilder source, bool isPreOp = true)
    {
        var dmlOp = typeof(DmlUnaryAssignment).FullName + "." +
                    Enum.GetName(typeof(DmlUnaryAssignment), DmlOperation.ParseUnaryAsn(op, isPreOp));

        return resolver => CreateUnAsn(op, subject(resolver), source(resolver), isPreOp);
    }

    public override ExpressionPieceBuilder VisitExpr_index([NotNull] DmlParser.Expr_indexContext c)
    {
        return CreateBin(DmlBinary.ArrayIndex, Visit(c.l), Visit(c.r));
    }

    public override ExpressionPieceBuilder VisitArray_basic_assignment(
        [NotNull] DmlParser.Array_basic_assignmentContext c)
    {
        return CreateTernery(DmlTernery.ArrayEmplace, Visit(c.dest), Visit(c.asn_idx.idx), Visit(c.src));
    }

    public override ExpressionPieceBuilder VisitArray_copyinto_assignment(
        [NotNull] DmlParser.Array_copyinto_assignmentContext c)
    {
        return CreateTernery(DmlTernery.ArrayEmplaceCopyInto, Visit(c.dest), Visit(c.asn_idx.idx), Visit(c.src));
    }

    public override ExpressionPieceBuilder VisitArray_augmented_assignment(
        [NotNull] DmlParser.Array_augmented_assignmentContext c)
    {
        var lhs_eval = CreateBin(DmlBinary.ArrayIndex, Visit(c.dest), Visit(c.asn_idx.idx));

        return CreateTernery(
            DmlTernery.ArrayEmplace,
            Visit(c.dest),
            Visit(c.asn_idx.idx),
            CreateBinAsn(
                c.op.GetText(),
                lhs_eval,
                Visit(c.src)
            )
        );
    }

    public override ExpressionPieceBuilder VisitArray_expr_lhs([NotNull] DmlParser.Array_expr_lhsContext context)
    {
        var lhs = Visit(context.expr_lhs());

        if (context.indexers != null)
        {
            foreach (var indexer in context.indexers.concrete_array_decl())
                lhs = CreateBin(DmlBinary.ArrayIndex, lhs, Visit(indexer.idx));
        }

        return lhs;
    }

    public override ExpressionPieceBuilder VisitArray_expr_unary_pre([NotNull] DmlParser.Array_expr_unary_preContext c)
    {
        return CreateUnAsnArrayOp(c.unop.GetText(), Visit(c.dest), Visit(c.asn_idx.idx), false);
    }

    public override ExpressionPieceBuilder VisitArray_expr_unary_post([NotNull] DmlParser.Array_expr_unary_postContext c)
    {
        return CreateUnAsnArrayOp(c.unop.GetText(), Visit(c.dest), Visit(c.asn_idx.idx), true);
    }

    //When we have an ExpressionSyntax, and we are not sure that it is a valid statement, we wrap it in a discard assignment
    public static StatementSyntax WrapEnsureStatement(ExpressionSyntax expr)
    {
        var newExpr = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_"),
                expr
            );

        return SyntaxFactory.ExpressionStatement(newExpr);
    }

    private ExpressionPieceBuilder CreateUnAsnArrayOp(string op, ExpressionPieceBuilder dest, ExpressionPieceBuilder idx, bool isPostOp)
    {
        var lhs_eval = CreateBin(DmlBinary.ArrayIndex, dest, idx);

        var asn_expr = CreateTernery(
            DmlTernery.ArrayEmplace,
            dest,
            idx,
            CreateUnAsn(
                op,
                lhs_eval,
                lhs_eval
            )
        );

        //In order to support post and pre unary assignment operaitons, we create an
        //expression using a tuple and the discard:
        // _ = (preValue, postValue)
        //Using just asn_expr, we only have direct access to the post value because the above asn_expr is wrapped in a Ternery
        //array emplace operation (and will therefore only return the value emplaced into the array [result of the final operation])
        //which only returns the post value. We generate a pre-value by evaluating prior to the post operation executing and placing it as the
        //first item in the tuple.

        //By using a tuple (and because the tuple arguments are evaluated left to right and the post/pre increment only occurs on evaluation of the latter argument
        //we can choose to evaluate the final expression as a post or pre op accessing either Item1 or Item2 of the tuple.
        //The discard assignment is to ensure that the resulting expression is also valid as a stand-alone statement.
        return r =>
        {
            var tpl = SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(new[] {
                    SyntaxFactory.Argument(lhs_eval(r)),
                    SyntaxFactory.Argument(asn_expr(r))
                }));

            var tupleReturnIdx = isPostOp ? "Item1" : "Item2";

            var expr = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tpl, SyntaxFactory.IdentifierName(tupleReturnIdx));

            //Because a tuple expression cannot stand its own as a statement (which may be the application of resulting expression syntax)
            //we simply create an assignment using the discard, which results in an expression which is also a statement.
            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_"),
                expr
            );
        };
    }

    public override ExpressionPieceBuilder VisitExpr_primitive_assert_type([NotNull] DmlParser.Expr_primitive_assert_typeContext context)
    {
        return (r) => CreatePrimitiveAssertType(Visit(context.left)(r), context.as_list().parameter_as_constraint().Select(i => i.GetText()).ToArray());
    }

    public override ExpressionPieceBuilder VisitExpr_unary_post([NotNull] DmlParser.Expr_unary_postContext c)
    {
        return CreateUnAsn(c.unop.GetText(), Visit(c.inner), Visit(c.inner), false);
    }

    public override ExpressionPieceBuilder VisitExpr_unary_pre([NotNull] DmlParser.Expr_unary_preContext c)
    {
        return CreateUnAsn(c.unop.GetText(), Visit(c.inner), Visit(c.inner), true);
    }

    public override ExpressionPieceBuilder VisitConfig_statement([NotNull] DmlParser.Config_statementContext context)
    {
        return base.VisitConfig_statement(context);
    }

    public override ExpressionPieceBuilder VisitExpr_int_literal(DmlParser.Expr_int_literalContext c)
    {
        return resolver => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(double.Parse(c.literal.Text)));
    }

    public override ExpressionPieceBuilder VisitExpr_dec_literal(DmlParser.Expr_dec_literalContext c)
    {
        return resolver => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(double.Parse(c.literal.Text)));
    }

    public override ExpressionPieceBuilder VisitExpr_dec_scientific_literal([NotNull] DmlParser.Expr_dec_scientific_literalContext c)
    {
        return resolver => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(double.Parse(c.literal.Text)));
    }

    public override ExpressionPieceBuilder VisitExpr_unary(DmlParser.Expr_unaryContext c)
    {
        return CreateUn(c.un_op().GetText(), Visit(c.inner));
    }

    public override ExpressionPieceBuilder VisitExpr_grouped(DmlParser.Expr_groupedContext c)
    {
        return resolver => SyntaxFactory.ParenthesizedExpression(this.Visit(c.inner)(resolver));
    }

    private ExpressionPieceBuilder BinExpr(string op, DmlParser.ExprContext l, DmlParser.ExprContext r)
    {
        return CreateBin(op, Visit(l), Visit(r));
    }

    public override ExpressionPieceBuilder VisitExpr_lhs_variable(DmlParser.Expr_lhs_variableContext context)
    {
        return resolver => Util.IdentifierName(context.lhs.GetText());
    }

    public override ExpressionPieceBuilder VisitExpr_lhs_prereturn([NotNull] Expr_lhs_prereturnContext context)
    {
        return r => CreatePrereturnExpression();
    }

    public override ExpressionPieceBuilder VisitBasic_assignment(DmlParser.Basic_assignmentContext context)
    {
        return CreateBinAsn("=", Visit(context.dest), Visit(context.src));
    }

    public override ExpressionPieceBuilder VisitAugmented_assignment(
        [NotNull] DmlParser.Augmented_assignmentContext context)
    {
        return CreateBinAsn(context.op.GetText(), Visit(context.dest), Visit(context.src));
    }

    public override ExpressionPieceBuilder VisitCopyinto_assignment(
        [NotNull] DmlParser.Copyinto_assignmentContext context)
    {
        return CreateBinAsn(":=", Visit(context.dest), Visit(context.src));
    }

    public override ExpressionPieceBuilder VisitExpr_arith_binary(DmlParser.Expr_arith_binaryContext c)
    {
        return BinExpr(c.op.GetText(), c.left, c.right);
    }

    public override ExpressionPieceBuilder VisitExpr_bit_binary([NotNull] DmlParser.Expr_bit_binaryContext c)
    {
        return BinExpr(c.op.GetText(), c.left, c.right);
    }

    public override ExpressionPieceBuilder VisitExpr_cmp_binary(DmlParser.Expr_cmp_binaryContext c)
    {
        return BinExpr(c.op.GetText(), c.left, c.right);
    }

    public override ExpressionPieceBuilder VisitExpr_logic_binary(DmlParser.Expr_logic_binaryContext c)
    {
        return BinExpr(c.op.GetText(), c.left, c.right);
    }

    public override ExpressionPieceBuilder VisitExpr_mul_binary(DmlParser.Expr_mul_binaryContext c)
    {
        return CreateBin(c.op.GetText(), Visit(c.left), Visit(c.right));
    }

    public ExpressionSyntax CreateCall(string name, IEnumerable<ArgumentSyntax> expr)
    {
        return
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName("ctx.ex.Invoke"),
                    SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                expr
                                    .Prepend(
                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name)))
                                    )
                                    .Prepend(
                                        SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.ParseName("GetExecutingContext"),
                                            SyntaxFactory.ArgumentList()))
                                    )
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(2))
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }

    public ExpressionPieceBuilder CreateCall(string name, IEnumerable<ArgumentPieceBuilder> expr)
    {
        return resolver =>
            CreateCall(name, expr.Select(n => n(resolver)));
    }

    public IEnumerable<ArgumentPieceBuilder> ParseArgumentList(DmlParser.Argument_listContext c)
    {
        return c.argument_list_item().Select(x =>
        {
            var inner = x.asn == null ? ((r) => ExpressionVisitor.CreateNull()) : Visit(x.asn);

            return new ArgumentPieceBuilder(
                e =>
                {
                    var arg = SyntaxFactory.Argument(inner(e));

                    ExpressionSyntax? argumentName = null;

                    if (x?.arg_name != null)
                        argumentName = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(x!.arg_name.GetText()));
                    else if (x?.arg_name_cplx != null)
                        argumentName = Visit(x!.arg_name_cplx)(e);

                    if (argumentName != null)
                    {

                        arg = SyntaxFactory.Argument(
                            SyntaxFactory.TupleExpression(
                                SyntaxFactory.SeparatedList(new[] {
                                    SyntaxFactory.Argument(argumentName!),
                                    arg
                                })
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.NamedArgumentTuple);
                    }
                    return arg;
                }
            );
        });
    }

    public override ExpressionPieceBuilder VisitMethod_call(DmlParser.Method_callContext c)
    {
        var rawArgs = ParseArgumentList(c.argument_list());

        return CreateCall(c.name.GetText(), rawArgs);
    }

    public override ExpressionPieceBuilder VisitIndirect_call(DmlParser.Indirect_callContext c)
    {
        var rawTargetArgs = ParseArgumentList(c.targetargs).ToList();
        var callArgs = ParseArgumentList(c.callargs).ToList();

        while (callArgs.Count < 2)
            callArgs.Add((_) => SyntaxFactory.Argument(ExpressionVisitor.CreateNull()));


        return CreateCall(RuntimeFrameworkIntrinsic.INDIRECT_CALL, callArgs.Concat(rawTargetArgs));
    }


    public override ExpressionPieceBuilder VisitExpr_stmnt_stub(DmlParser.Expr_stmnt_stubContext c)
    {
        var child = Visit(c.expr_complex().children.Single());
        return resolver => child(resolver);
    }

    //Need to be careful here, the string escape logic is different from "normal" string escaping.
    //I.e: In DML, \a needs to be preserved (it translates to A\An) and other edge cases. Can't use
    //built in string unescaping
    public string ParseEscapeString(string s)
    {
        s = s.Substring(1, s.Length - 2);
        StringBuilder unescaped = new StringBuilder();

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                // Handle escaped characters (e.g., \n, \t, \\)
                switch (s[i + 1])
                {
                    case 'n':
                        unescaped.Append('\n');
                        i++;
                        break;
                    case 'r':
                        unescaped.Append('\r');
                        i++;
                        break;
                    case 't':
                        unescaped.Append('\t');
                        i++;
                        break;
                    case '\\':
                        unescaped.Append('\\');
                        i++;
                        break;
                    case '"':
                        unescaped.Append('"');
                        i++;
                        break;
                    case '\'':
                        unescaped.Append('\'');
                        i++;
                        break;
                    default:
                        unescaped.Append(s[i]);
                        break;
                }
            }
            else
            {
                unescaped.Append(s[i]);
            }
        }

        return unescaped.ToString();
    }

    public override ExpressionPieceBuilder VisitExpr_string_literal(
        [NotNull] DmlParser.Expr_string_literalContext context)
    {
        var escapedString = ParseEscapeString(context.GetText());

        var strLiteralExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(escapedString));

        return _ => strLiteralExpr;
    }

    public override ExpressionPieceBuilder VisitInstance_call([NotNull] DmlParser.Instance_callContext c)
    {
        var rawArgs = ParseArgumentList(c.argument_list()).ToList();
        var subject = Visit(c.expr_lhs());
        return resolver =>
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName("ctx.ex.InvokeOn"),
                    SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                rawArgs.Select(y => y(resolver))
                                    .Prepend(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(c.name.GetText()))))
                                    .Prepend(SyntaxFactory.Argument(subject(resolver)))
                                    .Prepend(
                                        SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.ParseName("GetExecutingContext"),
                                            SyntaxFactory.ArgumentList()))
                                    )
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(3))
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke)
                .WithAdditionalAnnotations(BuilderAnnotations.DmlNativeDeferred);
    }

    public override ExpressionPieceBuilder VisitExpr_resource_identifier(
        [NotNull] DmlParser.Expr_resource_identifierContext context)
    {

        var escapedString = ParseEscapeString(context.GetText());
        return resolver =>
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.LoadResource"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(escapedString)
                                )
                            )
                        }
                    )
                )
            );
    }

    public static InvocationExpressionSyntax CreateResolveType(string typeName)
    {
        return CreateResolveType(SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(typeName)
        ));
    }

    public static InvocationExpressionSyntax CreateResolveType(ExpressionSyntax typeName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.ResolveType"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Argument(
                                typeName
                            )
                        }
                    )
                )
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke);
    }

    public override ExpressionPieceBuilder VisitExpr_type([NotNull] DmlParser.Expr_typeContext context)
    {
        return resolver => CreateResolveType(context.GetText());
    }

    public static ExpressionSyntax CreateSuperCall() => CreateSuperCall(Enumerable.Empty<ArgumentSyntax>());

    public static ExpressionSyntax CreateSuperCall(IEnumerable<ArgumentSyntax> args)
    {
        return
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("placeholder"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(args)
                )
            ).WithAdditionalAnnotations(BuilderAnnotations.CallBaseAnnotation);
    }

    public override ExpressionPieceBuilder VisitSuper_call([NotNull] DmlParser.Super_callContext c)
    {
        var rawArgs = ParseArgumentList(c.argument_list()).ToList();

        return resolver => CreateSuperCall(rawArgs.Select(y => y(resolver)));
    }

    public override ExpressionPieceBuilder VisitSelf_call([NotNull] DmlParser.Self_callContext c)
    {
        var rawArgs = ParseArgumentList(c.argument_list()).ToList();

        return resolver =>
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("placeholder"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(rawArgs.Select(y => y(resolver))) //.Select(SyntaxFactory.Argument))
                )
            ).WithAdditionalAnnotations(BuilderAnnotations.CallSelfAnnotation);
    }

    public override ExpressionPieceBuilder VisitExpr_lhs_property([NotNull] DmlParser.Expr_lhs_propertyContext c)
    {
        var lhs = Visit(c.expr_lhs());
        var property = c.identifier_name().GetText();

        return resolver =>
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                Util.DmlVariableIdentifier(lhs(resolver)),
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(property))
            );
    }

    public ExpressionSyntax CreateImplicitIsType(IdentifierNameSyntax subject, ExpressionSyntax? typeName = null)
    {
        var r = CreateExplicitIsType(subject, typeName, true);

        if (typeName == null)
            r = r.WithAdditionalAnnotations(BuilderAnnotations.CreateIsTypeWithoutType(subject.Identifier.Text));

        return r;
    }

    public static ExpressionSyntax CreateNull()
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(
                typeof(VarEnvObjectReference).FullName
            ),
            SyntaxFactory.Token(SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName("NULL")
        );
    }

    public override ExpressionPieceBuilder VisitNull_expr([NotNull] DmlParser.Null_exprContext context)
    {
        return resolver => CreateNull();
    }

    public ExpressionSyntax CreateExplicitIsType(ExpressionSyntax subject, ExpressionSyntax? typeName = null,
        bool allowMissingTypeArg = false)
    {
        var args = new List<ArgumentSyntax>(new[]
        {
            SyntaxFactory.Argument(
                subject
            )
        });

        if (typeName != null)
            args.Add(SyntaxFactory.Argument(typeName));
        else if (!allowMissingTypeArg)
            throw new Exception("Missing type hint.");

        var r = CreateCall("istype", args);

        return r;
    }

    public ExpressionSyntax CreateRuntimeFieldIsType(ExpressionSyntax subject, string field)
    {
        var args = new List<ArgumentSyntax>(new[]
        {
            SyntaxFactory.Argument(subject),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(field)))
        });

        var r = CreateCall(RuntimeFrameworkIntrinsic.INDIRECT_ISTYPE, args);

        return r;
    }

    public override ExpressionPieceBuilder VisitExpr_istype_local([NotNull] DmlParser.Expr_istype_localContext c)
    {
        return resolver => CreateImplicitIsType(Util.IdentifierName(c.varname.GetText()),
            c.typename == null ? null : Visit(c.typename)(resolver));
    }

    public override ExpressionPieceBuilder VisitExpr_istype_property([NotNull] DmlParser.Expr_istype_propertyContext c)
    {
        return resolver =>
            CreateExplicitIsType(Visit(c.varname)(resolver), c.typename == null ? null : Visit(c.typename)(resolver));
    }

    public override ExpressionPieceBuilder VisitExpr_implicit_istype_property([NotNull] DmlParser.Expr_implicit_istype_propertyContext c)
    {
        return resolver => CreateRuntimeFieldIsType(Visit(c.varname)(resolver), c.identifier_name().GetText());
    }

    public override ExpressionPieceBuilder VisitExpr_property([NotNull] DmlParser.Expr_propertyContext c)
    {
        var lhs = Visit(c.expr());
        var property = c.identifier_name().GetText();

        return resolver =>
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                lhs(resolver),
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(property))
            );
    }

    public override ExpressionPieceBuilder VisitNew_call_arridx_operand(
        [NotNull] DmlParser.New_call_arridx_operandContext c)
    {
        return resolver => CreateResolveType(CreateBin(DmlBinary.ArrayIndex, Visit(c.l), Visit(c.r))(resolver));
    }

    public override ExpressionPieceBuilder VisitNew_call_expr_eval(
        [NotNull] DmlParser.New_call_expr_evalContext context)
    {
        return Visit(context.expr_eval);
    }

    public override ExpressionPieceBuilder VisitNew_call_type_literal(
        [NotNull] DmlParser.New_call_type_literalContext context)
    {
        return resolver => CreateResolveType(context.object_tree_path_expr().GetText());
    }

    public List<Tuple<string, ExpressionPieceBuilder>> ParseFieldInitExpression(New_call_field_initializer_listContext ctx)
    {
        return ctx.new_call_field_initializer().Select(x =>
                Tuple.Create(
                    x.identifier_name().GetText(),
                    Visit(x.expr())
                )
            )
            .ToList();
    }

    public ExpressionSyntax WrapFieldInitializer(ExpressionSyntax newExpression, IEnumerable<Tuple<string, ExpressionSyntax>> fieldInitializers)
    {
        var args = new List<ArgumentSyntax>();

        args.Add(SyntaxFactory.Argument(newExpression));

        foreach (var p in fieldInitializers)
        {
            args.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(p.Item1))));
            args.Add(SyntaxFactory.Argument(p.Item2));
        }

        return CreateCall(RuntimeFrameworkIntrinsic.FIELD_LIST_INIT, args);
    }

    public ExpressionPieceBuilder WrapFieldInitializer(ExpressionPieceBuilder newExpression, IEnumerable<Tuple<string, ExpressionPieceBuilder>> fieldInitializers)
    {
        return r => WrapFieldInitializer(newExpression(r), fieldInitializers.Select(f => Tuple.Create(f.Item1, f.Item2(r))));
    }

    public ExpressionPieceBuilder WrapFieldInitializer(ExpressionPieceBuilder newExpression, DmlParser.New_call_field_initializer_listContext ctx)
    {
        var args = ctx.new_call_field_initializer().Select(x =>
                Tuple.Create(
                    x.identifier_name().GetText(),
                    Visit(x.expr())
                )
            )
            .ToList();

        return WrapFieldInitializer(newExpression, args);
    }

    public override ExpressionPieceBuilder VisitNew_call_explicit([NotNull] DmlParser.New_call_explicitContext c)
    {
        ExpressionPieceBuilder typeHint;

        if (c.type_hint_eval != null)
        {
            typeHint = Visit(c.type_hint_eval);
        }
        else
        {
            if (!typeHintCtx.TryPeek(out var typeNameHint))
                throw new Exception("New call has no type hints!");

            typeHint = resolver =>
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(typeNameHint));
        }

        var rawArgs = c.argument_list() == null
            ? new List<ArgumentPieceBuilder>()
            : ParseArgumentList(c.argument_list()).ToList();

        ExpressionPieceBuilder newExpr = (resolver) =>
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName("ctx.NewAtomic"),
                    SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                rawArgs.Select(y => y(resolver))
                                    .Prepend(SyntaxFactory.Argument(typeHint(resolver)))
                            )
                        )
                        .WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(1))
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke);

        if(c.new_call_field_initializer_list() != null)
            newExpr = WrapFieldInitializer(newExpr, c.new_call_field_initializer_list());

        return newExpr;
    }

    public void PushTypeHint(string objType)
    {
        typeHintCtx.Push(objType);
    }

    public void PopTypeHint()
    {
        typeHintCtx.Pop();
    }

    public ExpressionSyntax CreateListLiteral(IEnumerable<ExpressionSyntax> initializer)
    {
        var target = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.ListLiteral"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        initializer.Select(SyntaxFactory.Argument)
                    )
                ).WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(0))
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke);

        return target;
    }

    public ExpressionSyntax CreateAssocListLiteral(IEnumerable<Tuple<ExpressionSyntax, ExpressionSyntax>> initializer)
    {
        var flattenedArgList = initializer.SelectMany(x => new[] { x.Item1, x.Item2 }).ToList();

        var target = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("ctx.AssocListLiteral"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        flattenedArgList.Select(SyntaxFactory.Argument)
                    )
                ).WithAdditionalAnnotations(BuilderAnnotations.CreateManagedArgsAnnotation(0))
            )
            .WithAdditionalAnnotations(BuilderAnnotations.DmlInvoke);

        return target;
    }

    public override ExpressionPieceBuilder VisitExpr_assoc_list_literal([NotNull] DmlParser.Expr_assoc_list_literalContext context)
    {
        var kvPairs = context
            .assoc_list_expr()
            .assoc_list_expr_kv_pair()
            .Select(x => 
                Tuple.Create(
                    Visit(x.key),
                    Visit(x.value)
                )
            );

        return resolver => CreateAssocListLiteral(kvPairs.Select(x => Tuple.Create(x.Item1(resolver), x.Item2(resolver))));
    }

    public override ExpressionPieceBuilder VisitExpr_list_literal([NotNull] DmlParser.Expr_list_literalContext c)
    {
        var rawElements = c.list_expr() == null
            ? new List<ExpressionPieceBuilder>()
            : c.list_expr().expr().Select(Visit).ToList();

        return resolver => CreateListLiteral(rawElements.Select(e => e(resolver)));
    }

    public override ExpressionPieceBuilder VisitList_range_expr([NotNull] DmlParser.List_range_exprContext context)
    {
        return resolver => {
            var args = new[] {
                SyntaxFactory.Argument(Visit(context.start)(resolver)),
                SyntaxFactory.Argument(Visit(context.end)(resolver))
            }.ToList();

            if (context.step != null)
                args.Add(SyntaxFactory.Argument(Visit(context.step)(resolver)));

            return CreateCall(RuntimeFrameworkIntrinsic.GENERATE_RANGE, args);
        };
    }

    public override ExpressionPieceBuilder VisitPick_expression([NotNull] DmlParser.Pick_expressionContext context)
    {
        return (resolver) =>
        {
            var args = context.pick_expr().pick_expr_pair().SelectMany(p =>
            {
                var probVal = p.prob == null ? 100 : int.Parse(p.prob.Text);
                var prob = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(probVal));

                return new[]
                {
                    SyntaxFactory.Argument(prob),
                    SyntaxFactory.Argument(Visit(p.val)(resolver))
                };
            });

            return CreateCall(RuntimeFrameworkIntrinsic.PICK_WEIGHTED, args);
        };
    }

    public override ExpressionPieceBuilder VisitExpr_prereturn(DmlParser.Expr_prereturnContext context)
    {
        return (resolver) => CreatePrereturnExpression();
    }

    public static ExpressionSyntax CreatePrereturnExpression()
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ThisExpression(),
            SyntaxFactory.IdentifierName("ImplicitReturn")
        ).WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation);
    }

    public override ExpressionPieceBuilder VisitExpr_hexnumber([NotNull] Expr_hexnumberContext c)
    {
        var hexString = c.literal.Text.TrimStart(new[] { 'x', 'X', '0' });
        var decValue = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
        return resolver => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(decValue));
    }

    public override ExpressionPieceBuilder VisitPrereturn_array_augassignment([NotNull] Prereturn_array_augassignmentContext c)
    {
        var lhs_eval = CreateBin(DmlBinary.ArrayIndex, r => CreatePrereturnExpression(), Visit(c.asn_idx.idx));

        return CreateTernery(
            DmlTernery.ArrayEmplace,
            r => CreatePrereturnExpression(),
            Visit(c.asn_idx.idx),
            CreateBinAsn(
                c.op.GetText(),
                lhs_eval,
                Visit(c.src)
            )
        );
    }


    public override ExpressionPieceBuilder VisitPrereturn_array_assignment([NotNull] Prereturn_array_assignmentContext c)
    {
        var lhs_eval = CreateBin(DmlBinary.ArrayIndex, r => CreatePrereturnExpression(), Visit(c.asn_idx.idx));

        return CreateTernery(
            DmlTernery.ArrayEmplace,
            r => CreatePrereturnExpression(),
            Visit(c.asn_idx.idx),
            Visit(c.src)
        );
    }

    public override ExpressionPieceBuilder VisitPrereturn_expr_unary_post([NotNull] Prereturn_expr_unary_postContext c)
    {
        return r => CreateUnAsn(c.unop.GetText(), CreatePrereturnExpression(), CreatePrereturnExpression(), false).WithAdditionalAnnotations(BuilderAnnotations.ImplicitReturnAssignment);
    }

    public override ExpressionPieceBuilder VisitPrereturn_expr_unary_pre([NotNull] Prereturn_expr_unary_preContext c)
    {
        return r => CreateUnAsn(c.unop.GetText(), CreatePrereturnExpression(), CreatePrereturnExpression(), true).WithAdditionalAnnotations(BuilderAnnotations.ImplicitReturnAssignment);
    }
    
    public override ExpressionPieceBuilder VisitPrereturn_simple_assignment([NotNull] Prereturn_simple_assignmentContext context)
    {
        return r => CreateAssignment(CreatePrereturnExpression(), Visit(context.expr())(r)).WithAdditionalAnnotations(BuilderAnnotations.ImplicitReturnAssignment);
    }

    public override ExpressionPieceBuilder VisitPrereturn_augasnop([NotNull] Prereturn_augasnopContext c)
    {
        return r => CreateBinAsn(c.augAsnOp().GetText(), CreatePrereturnExpression(), Visit(c.src)(r)).WithAdditionalAnnotations(BuilderAnnotations.ImplicitReturnAssignment);
    }
}