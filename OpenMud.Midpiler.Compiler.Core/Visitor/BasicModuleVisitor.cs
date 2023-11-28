using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;

namespace OpenMud.Mudpiler.Compiler.Core.Visitor;

public class BasicModuleVisitor : DmlParserBaseVisitor<IModulePieceBuilder>
{
    private readonly CodeSuiteVisitor CODE;
    private static readonly ExpressionVisitor EXPR = new();

    private int methodDefinitionOrder;

    private readonly SourceMapping mapping;

    internal BasicModuleVisitor(SourceMapping mapping)
    {
        this.mapping = mapping;
        this.CODE = new(mapping);
    }

    public BasicModuleVisitor()
    {
        mapping = new SourceMapping(Enumerable.Empty<IToken>());
        this.CODE = new(mapping);
    }

    private static ParameterSyntax CreateParameter(DmlParser.ParameterContext c, IDreamMakerSymbolResolver resolver)
    {
        var name = c.name.GetText();
        TypeSyntax? type = null;

        var typeHint = c.object_ref_type == null ? "" : c.reference_object_tree_path().GetText();

        type = BuiltinTypes.ResolveGenericType();

        var init = c.init == null ? null : SyntaxFactory.EqualsValueClause(EXPR.Visit(c.init)(resolver));

        return SyntaxFactory.Parameter(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(),
            type,
            SyntaxFactory.Identifier(name),
            init
        ).WithAdditionalAnnotations(BuilderAnnotations.CreateTypeHints(typeHint));
    }

    public override IModulePieceBuilder VisitObject_binop_override_definition(
        [NotNull] DmlParser.Object_binop_override_definitionContext c)
    {
        var op = c.@operator.GetText();
        var operation = DmlOperation.ParseBinary(op);
        var operationName = Enum.GetName(typeof(DmlBinary), operation);
        var fullName = c.name.GetText().Trim() + "_" + operationName;

        var attributeName = typeof(BinOpOverride).FullName;
        var attributeArgumet = typeof(DmlBinary).FullName + "." + operationName;

        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.ParseName(attributeArgumet)
                    )
                })
            )
        );

        return CreateFunction(fullName, c.non_empty_parameter_list().parameter(), c.body, new[] { attribute });
    }

    public override IModulePieceBuilder VisitObject_unop_override_definition(
        [NotNull] DmlParser.Object_unop_override_definitionContext c)
    {
        var op = c.@operator.GetText();
        var operation = DmlOperation.ParseUnary(op);
        var operationName = Enum.GetName(typeof(DmlUnary), operation);
        var fullName = c.name.GetText().Trim() + "_" + operationName;

        var attributeName = typeof(UnOpOverride).FullName;
        var attributeArgumet = typeof(DmlUnary).FullName + "." + operationName;

        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.ParseName(attributeArgumet)
                    )
                })
            )
        );

        return CreateFunction(fullName, new DmlParser.ParameterContext[0], c.body, new[] { attribute });
    }

    public override IModulePieceBuilder VisitObject_copyInto_override_definition(
        [NotNull] DmlParser.Object_copyInto_override_definitionContext c)
    {
        var op = c.@operator.Text;
        var operation = DmlOperation.ParseBinaryAsn(op);
        var operationName = Enum.GetName(typeof(DmlBinaryAssignment), operation);
        var fullName = c.name.GetText().Trim() + "_" + operationName;

        var attributeName = typeof(BinOpAsnOverride).FullName;
        var attributeArgumet = typeof(DmlBinaryAssignment).FullName + "." + operationName;

        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.ParseName(attributeArgumet)
                    )
                })
            )
        );

        return CreateFunction(fullName, c.parameter_list().parameter(), c.body, new[] { attribute });
    }

    public override IModulePieceBuilder VisitObject_augasn_override_definition(
        [NotNull] DmlParser.Object_augasn_override_definitionContext c)
    {
        var op = c.@operator.GetText();
        var operation = DmlOperation.ParseBinaryAsn(op);
        var operationName = Enum.GetName(typeof(DmlBinaryAssignment), operation);
        var fullName = c.name.GetText().Trim() + "_" + operationName;

        var attributeName = typeof(BinOpAsnOverride).FullName;
        var attributeArgumet = typeof(DmlBinaryAssignment).FullName + "." + operationName;

        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.ParseName(attributeArgumet)
                    )
                })
            )
        );

        return CreateFunction(fullName, c.parameter_list().parameter(), c.body, new[] { attribute });
    }

    public override IModulePieceBuilder VisitObject_unopasn_override_definition(
        [NotNull] DmlParser.Object_unopasn_override_definitionContext c)
    {
        var op = c.@operator.GetText();

        var operationNames = new[]
            {
                DmlOperation.ParseUnaryAsn(op, true),
                DmlOperation.ParseUnaryAsn(op, false)
            }
            .Select(n => Enum.GetName(typeof(DmlUnaryAssignment), n));

        var fullName = c.name.GetText().Trim() + "_" + string.Join("_", operationNames);

        var attributeArguments =
            operationNames
                .Select(n => typeof(DmlUnaryAssignment).FullName + "." + n)
                .Select(n =>
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.ParseName(n)
                    )
                );

        var attributeName = typeof(UnOpAsnpOverride).FullName;

        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(attributeArguments)
            )
        );

        return CreateFunction(fullName, c.parameter_list().parameter(), c.body, new[] { attribute });
    }

    private IModulePieceBuilder CreateFunction(string fullName, IEnumerable<DmlParser.ParameterContext> parameters,
        DmlParser.SuiteContext body, AttributeSyntax[] attr = null)
    {
        Func<StatementSyntax, BlockSyntax> warpWithBlock =
            s => s is BlockSyntax syntax ? syntax : SyntaxFactory.Block(s);

        var bodyBuilder = CODE.Visit(body);

        MethodBodyPieceBuilder builder = (resolver, method) =>
        {
            List<AttributeSyntax> allAttributes = new();

            if (attr != null)
                allAttributes.AddRange(attr);

            allAttributes.AddRange(ParametersConstraintBuilder.CreateConstraintAttribute(parameters)(resolver));

            method =
                method
                    .WithBody(warpWithBlock(CodeSuiteVisitor.GroupStatements(bodyBuilder(resolver))))
                    .WithReturnType(SyntaxFactory.ParseTypeName("dynamic"))
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                        parameters.Select(p => CreateParameter(p, resolver))
                    )));

            if (allAttributes.Any())
                method =
                    method
                        .WithAttributeLists(
                            SyntaxFactory.List(new[]
                                {
                                    SyntaxFactory.AttributeList(
                                        SyntaxFactory.SeparatedList(
                                            allAttributes
                                        )
                                    )
                                }
                                .Concat(method.AttributeLists)
                            )
                        );

            return method;
        };

        var declOrder = methodDefinitionOrder++;

        var settings = new ScopedClassPieceBuilder(fullName, new SettingVisitor(declOrder).Visit(body));

        return new CompositeClassPieceBuilder(new IModulePieceBuilder[]
        {
            new MethodPieceBuilder(builder, fullName, declOrder),
            settings
        });
    }

    public override IModulePieceBuilder VisitObject_function_definition(DmlParser.Object_function_definitionContext c)
    {
        var fullName = c.name.GetText();

        return CreateFunction(fullName, c.parameter_list().parameter(), c.body);
    }

    public override IModulePieceBuilder VisitObject_tree_var_suite(
        [NotNull] DmlParser.Object_tree_var_suiteContext context)
    {
        return base.VisitObject_tree_var_suite(context);
    }

    public override IModulePieceBuilder VisitObject_tree_definition(DmlParser.Object_tree_definitionContext context)
    {
        var builders = new List<IModulePieceBuilder>();

        if (context.scope != null)
            builders.Add(new TouchPieceBuilder(context.scope.GetText()));

        if (context.inner != null)
            builders.Add(new ScopedClassPieceBuilder(context.scope.GetText(), Visit(context.inner)));

        if (context.vars_inner != null)
        {
            var varBuilders = new List<IModulePieceBuilder>();
            var modifier = context.modifier?.GetText();

            var varDecl = context.vars_inner.NAME().Select(x => x.GetText()).ToList();
            varDecl.AddRange(context.vars_inner.initializer_assignment().Select(x => x.path.GetText()));

            varBuilders.AddRange(context.vars_inner.implicit_variable_declaration().Select(Visit));
            varBuilders.AddRange(context.vars_inner.initializer_assignment().Select(Visit));
            
            varBuilders.AddRange(varDecl.Select(CreateFieldDeclaration));

            if (modifier != null)
            {
                var scope = $"{modifier}/";
                builders.AddRange(varBuilders.Select(v => new ScopedClassPieceBuilder(scope, v)));
            }
            else
                builders.AddRange(varBuilders);
        }

        return new CompositeClassPieceBuilder(builders);
    }

    public override IModulePieceBuilder VisitDml_module(DmlParser.Dml_moduleContext context)
    {
        methodDefinitionOrder = 0;

        var parseOrder =
            context.object_function_definition().Cast<ParserRuleContext>()
                .Concat(context.object_tree_definition())
                .Concat(context.variable_declaration())
                .Concat(context.variable_set_declaration())
                .Concat(context.initializer_assignment())
                .OrderBy(x => x.Start.Line);

        return new CompositeClassPieceBuilder(
            parseOrder.Select(Visit)
        );
    }

    public override IModulePieceBuilder VisitVariable_set_declaration([NotNull] DmlParser.Variable_set_declarationContext context)
    {
        var prefix = context.path_prefix?.GetText();
        var decls = new List<DmlParser.Implicit_variable_declarationContext>();

        if (context.varset_suite != null)
            decls.AddRange(context.varset_suite.implicit_variable_declaration());

        if (context.varset_comma_suite != null)
            decls.AddRange(context.varset_comma_suite.implicit_variable_declaration().ToList());

        var typedDecl = decls
            .Select(p => p.implicit_typed_variable_declaration())
            .Where(p => p != null)
            .Select(p => CODE.ParseVariableDeclaration(p, prefix));

        var untypedDecl = decls
                    .Select(p => p.implicit_untyped_variable_declaration())
                    .Where(p => p != null)
                    .Select(p => CODE.ParseVariableDeclaration(p, prefix));

        var builders = typedDecl
            .Concat(untypedDecl)
            .Select(CreateBuilder);
        
        return new CompositeClassPieceBuilder(builders);
    }

    public override IModulePieceBuilder VisitObject_tree_suite(DmlParser.Object_tree_suiteContext context)
    {
        return new CompositeClassPieceBuilder(
            context.object_tree_stmt().Select(Visit)
        );
    }

    public override IModulePieceBuilder VisitObject_tree_stmt(DmlParser.Object_tree_stmtContext c)
    {
        if (c.object_function_definition() != null)
            return Visit(c.object_function_definition());

        if (c.object_tree_definition() != null)
            return Visit(c.object_tree_definition());

        if (c.initializer_assignment() != null)
            return Visit(c.initializer_assignment());

        if (c.variable_declaration() != null)
            return Visit(c.variable_declaration());


        if (c.object_augasn_override_definition() != null)
            return Visit(c.object_augasn_override_definition());

        if (c.object_binop_override_definition() != null)
            return Visit(c.object_binop_override_definition());

        if (c.object_unop_override_definition() != null)
            return Visit(c.object_unop_override_definition());

        if (c.object_copyInto_override_definition() != null)
            return Visit(c.object_copyInto_override_definition());

        if (c.object_unopasn_override_definition() != null)
            return Visit(c.object_unopasn_override_definition());

        if(c.variable_set_declaration() != null)
            return Visit(c.variable_set_declaration());

        throw new Exception("Unhandled tree statement.");
    }

    public override IModulePieceBuilder VisitInitializer_assignment(DmlParser.Initializer_assignmentContext context)
    {
        FieldInitBodyPieceBuilder exprBuilder = resolver => EXPR.Visit(context.expr())(resolver);
        return new FieldInitPieceBuilder(context.path.GetText(), exprBuilder);
    }

    private IModulePieceBuilder CreateFieldDeclaration(string name)
    {
        FieldBodyPieceBuilder builder = (resolver, fld) =>
            fld.WithDeclaration(
                fld.Declaration
                    .WithType(BuiltinTypes.ResolveGenericType())
                    .WithVariables(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            fld.Declaration.Variables.Single()
                        })
                    )
            );

        var fldBuilder = new FieldPieceBuilder(builder, "", name);

        return new CompositeClassPieceBuilder(new IModulePieceBuilder[]
        {
            fldBuilder
        });
    }

    public override IModulePieceBuilder VisitImplicit_untyped_variable_declaration(
        [NotNull] DmlParser.Implicit_untyped_variable_declarationContext context)
    {
        var r = CODE.ParseVariableDeclaration(context);

        return CreateBuilder(r);
    }

    public IModulePieceBuilder CreateBuilder(VariableDeclarationMetadata r)
    {
        FieldBodyPieceBuilder builder = (resolver, fld) =>
            fld.WithDeclaration(
                fld.Declaration
                    .WithType(r.varType)
                    .WithVariables(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            fld.Declaration.Variables.Single()
                        })
                    )
            );

        var fldBuilder = new FieldPieceBuilder(builder, r.objectType == null ? "" : r.objectType, r.variableName);

        if (r.init == null)
            return fldBuilder;

        return new CompositeClassPieceBuilder(new IModulePieceBuilder[]
        {
            fldBuilder,
            new FieldInitPieceBuilder(r.variableName, resolver => r.init(resolver))
        });
    }

    public override IModulePieceBuilder VisitImplicit_typed_variable_declaration(
        [NotNull] DmlParser.Implicit_typed_variable_declarationContext context)
    {
        var r = CODE.ParseVariableDeclaration(context);

        return CreateBuilder(r);
    }
}