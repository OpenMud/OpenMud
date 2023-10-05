using System.Collections.Immutable;
using Antlr4.Runtime.Misc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;

namespace OpenMud.Mudpiler.Compiler.Core.Visitor;

public static class ParametersConstraintBuilder
{
    private static readonly string ArgAsConstraintPath = typeof(ArgAsConstraint).FullName;
    private static readonly string ArgAsPath = typeof(ArgAs).FullName;

    private static AttributeSyntax CompileArgAsConstraint(int argIdx, int rank, ArgAs p)
    {
        return SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(ArgAsConstraintPath),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(argIdx))),
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(rank))),
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.ParseExpression(ArgAsPath + "." + Enum.GetName(typeof(ArgAs), p)))
                })
            )
        );
    }

    public static Func<IDreamMakerSymbolResolver, IEnumerable<AttributeSyntax>> CreateConstraintAttribute(
        IEnumerable<DmlParser.ParameterContext> parameters)
    {
        return resolver =>
        {
            var argConstraints = new List<AttributeSyntax>();

            for (var i = 0; i < parameters.Count(); i++)
            {
                var p = parameters.ElementAt(i);

                List<ArgAs> asConstraints = new();

                if (p.as_constraints != null)
                    asConstraints.AddRange(p.as_constraints.parameter_as_constraint()
                        .Select(x => (ArgAs)Enum.Parse(typeof(ArgAs), x.GetText(), true)));

                if (asConstraints.Count == 0)
                    asConstraints.Add(ArgAs.Anything);

                for (var x = 0; x < asConstraints.Count; x++)
                    argConstraints.Add(CompileArgAsConstraint(i, x, asConstraints[x]));

                if (p.set_constraints != null)
                    argConstraints.Add(new ConstraintSetResolver(i).Visit(p.set_constraints)(resolver));
            }


            return argConstraints;
        };
    }

    private class ConstraintSetResolver : DmlParserBaseVisitor<Func<IDreamMakerSymbolResolver, AttributeSyntax>>
    {
        private static readonly string SimpleAnnotationPath = typeof(SimpleSourceConstraint).FullName;
        private static readonly string ListEvalSourceConstraintPath = typeof(ListEvalSourceConstraint).FullName;

        private static readonly ExpressionVisitor EXPR = new();

        public static readonly IImmutableDictionary<SourceType, string> SourceNames =
            Enum.GetNames(typeof(SourceType))
                .ToImmutableDictionary(
                    x => (SourceType)Enum.Parse(typeof(SourceType), x),
                    x => typeof(SourceType).FullName + "." + x
                );

        private readonly int argIdx;

        public ConstraintSetResolver(int argIdx)
        {
            this.argIdx = argIdx;
        }

        private static AttributeSyntax CreateSimple(int argIdx, SourceType type, int operand = 0)
        {
            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(SimpleAnnotationPath),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(argIdx)
                            )
                        ),
                        SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(SourceNames[type])),
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(operand)
                            )
                        )
                    })
                )
            );
        }

        public static AttributeSyntax CreateEvalList(int argIdx, IEnumerable<string> argList)
        {
            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(ListEvalSourceConstraintPath),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList(
                        argList.Select(a =>
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(a)
                                    )
                                )
                            )
                            .Prepend(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(argIdx)
                                    )
                                )
                            )
                    )
                )
            );
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_usr_contents(
            [NotNull] DmlParser.Parameter_constraint_set_usr_contentsContext context)
        {
            return resolver => CreateSimple(argIdx, SourceType.UserContents);
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_usr_group(
            [NotNull] DmlParser.Parameter_constraint_set_usr_groupContext context)
        {
            return resolver => CreateSimple(argIdx, SourceType.UserGroup);
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_usr_loc(
            [NotNull] DmlParser.Parameter_constraint_set_usr_locContext context)
        {
            return resolver => CreateSimple(argIdx, SourceType.UserLoc);
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_inworld(
            [NotNull] DmlParser.Parameter_constraint_set_inworldContext context)
        {
            return resolver => CreateSimple(argIdx, SourceType.World);
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_oview(
            [NotNull] DmlParser.Parameter_constraint_set_oviewContext context)
        {
            return resolver => CreateSimple(argIdx, SourceType.OView, int.Parse(context.arg.Text));
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_view(
            [NotNull] DmlParser.Parameter_constraint_set_viewContext context)
        {
            return resolver => CreateSimple(argIdx, SourceType.View, int.Parse(context.arg.Text));
        }

        private static string CreateEvalArg(IDreamMakerSymbolResolver resolver, ArgumentSyntax expr)
        {
            var supportMethodName = resolver.DefineSupportMethod(
                "",
                m => m.WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(expr.Expression)
                    )
                )
            );

            return supportMethodName;
        }

        public override Func<IDreamMakerSymbolResolver, AttributeSyntax> VisitParameter_constraint_set_list_list_eval(
            [NotNull] DmlParser.Parameter_constraint_set_list_list_evalContext context)
        {
            return resolver =>
            {
                var argExprs = EXPR.ParseArgumentList(context.argument_list()).Select(e =>
                    CreateEvalArg(resolver, e(resolver))
                );

                return CreateEvalList(argIdx, argExprs);
            };
        }
    }
}