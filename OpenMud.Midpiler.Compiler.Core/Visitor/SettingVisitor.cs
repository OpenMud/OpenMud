using Antlr4.Runtime.Misc;
using Microsoft.CodeAnalysis.CSharp;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;

namespace OpenMud.Mudpiler.Compiler.Core.Visitor;

public class SettingVisitor : DmlParserBaseVisitor<IModulePieceBuilder>
{
    private readonly int methodDeclarationOrder;
    private readonly ExpressionVisitor EXPR = new();

    public SettingVisitor(int methodDeclarationOrder)
    {
        this.methodDeclarationOrder = methodDeclarationOrder;
    }

    public override IModulePieceBuilder VisitIf_stmt(DmlParser.If_stmtContext c)
    {
        return new CompositeClassPieceBuilder(c.suite().Select(Visit).Where(x => x != null));
    }

    public override IModulePieceBuilder VisitSuite_single_stmt(DmlParser.Suite_single_stmtContext c)
    {
        return Visit(c.simple_stmt());
    }

    public override IModulePieceBuilder VisitSuite_multi_stmt(DmlParser.Suite_multi_stmtContext c)
    {
        return new CompositeClassPieceBuilder(c.stmt().Select(Visit).Where(x => x != null));
    }

    public override IModulePieceBuilder VisitSimple_stmt(DmlParser.Simple_stmtContext context)
    {
        return Visit(context.small_stmt());
    }

    public override IModulePieceBuilder VisitSuite_empty([NotNull] DmlParser.Suite_emptyContext context)
    {
        return new NullModulePieceBuilder();
    }

    public override IModulePieceBuilder VisitConfig_statement([NotNull] DmlParser.Config_statementContext context)
    {
        return new MethodSettingPieceBuilder(context.cfg_key.GetText(), methodDeclarationOrder,
            resolver =>
                a => a.AddArgumentListArguments(
                    SyntaxFactory.AttributeArgument(EXPR.Visit(context.cfg_value)(resolver)))
        );
    }

    private IModulePieceBuilder DefineSrc(SourceType src, int argument)
    {
        return new MethodSettingPieceBuilder("src", methodDeclarationOrder,
            resolver =>
                a => a.WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(typeof(SourceType).FullName),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(src.ToString())
                                )
                            ),
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(argument))
                            )
                        })
                    )
                )
        );
    }

    private IModulePieceBuilder DefineSrc(SourceType src)
    {
        return new MethodSettingPieceBuilder("src", methodDeclarationOrder,
            resolver =>
                a => a.WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(typeof(SourceType).FullName),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(src.ToString())
                                )
                            )
                        })
                    )
                )
        );
    }

    public override IModulePieceBuilder VisitSet_src_contents([NotNull] DmlParser.Set_src_contentsContext context)
    {
        return DefineSrc(SourceType.UserContents);
    }

    public override IModulePieceBuilder VisitSet_src_group([NotNull] DmlParser.Set_src_groupContext context)
    {
        return DefineSrc(SourceType.UserGroup);
    }

    public override IModulePieceBuilder VisitSet_src_loc([NotNull] DmlParser.Set_src_locContext context)
    {
        return DefineSrc(SourceType.UserLoc);
    }

    //set src = usr -> Means must be equal to user
    //set src in usr -> usr.contents ;
    public override IModulePieceBuilder VisitSet_src_user([NotNull] DmlParser.Set_src_userContext context)
    {
        return context.GetText().Contains("=") ? DefineSrc(SourceType.User) : DefineSrc(SourceType.UserContents);
    }

    public override IModulePieceBuilder VisitSet_src_view([NotNull] DmlParser.Set_src_viewContext context)
    {
        return DefineSrc(SourceType.View, int.Parse(context.arg.Text));
    }

    public override IModulePieceBuilder VisitSet_src_oview([NotNull] DmlParser.Set_src_oviewContext context)
    {
        return DefineSrc(SourceType.OView, int.Parse(context.arg.Text));
    }
}