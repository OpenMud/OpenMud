using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using FieldDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

public class DebuggableProcRewriter : CSharpSyntaxRewriter
{
    private string? sourceFileName = null;
    private HashSet<int> executableLines = new();

    private void SetSourceFileName(string mappedName)
    {
        if (sourceFileName == null)
        {
            sourceFileName = mappedName;
        } else if (!sourceFileName.Equals(mappedName))
        {
            throw new Exception(
                "Error, function consists of statements sourced from multiple different files? Unsupported semantic."
            );
        }
    }
    
    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        if (BuilderAnnotations.GetSourceMap(node, out var mappedName, out int line))
        {
            SetSourceFileName(mappedName);

            executableLines.Add(line);
            
            var invokeExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(nameof(IDebuggableProc.Step)),
                    SyntaxFactory.IdentifierName(".Invoke")
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(line)))
                    })
                )
            );
            
            node = node.WithStatements(
                node.Statements.Insert(0, SyntaxFactory.ExpressionStatement(invokeExpression))
            );
        }
        
        return base.VisitBlock(node);
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        //Only apply to the proc context class (this is where the actual executable code is.)
        if(!BuilderAnnotations.HasDmlProcContextClassAnnotation(node))
            return node;

        sourceFileName = null;
        
        //Apply the visit to collect the source information.
        node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

        //No debuggable lines.
        if (sourceFileName == null)
            return node;
        
        // Implement the debuggable interface.
        node = node.AddBaseListTypes(
            SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeof(IDebuggableProc).FullName!))
        );
        
        //Add the static step and fault fields.

        //Add Step event
        node = node.AddMembers(SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(typeof(ProcStep).FullName!)
            ).AddVariables(SyntaxFactory.VariableDeclarator("Step"))
        ).AddModifiers(
            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
            SyntaxFactory.Token(SyntaxKind.EventKeyword)
        ));

        //Add Fault Event
        node = node.AddMembers(SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(typeof(ProcFault).FullName!)
            ).AddVariables(SyntaxFactory.VariableDeclarator("Fault"))
        ).AddModifiers(
            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
            SyntaxFactory.Token(SyntaxKind.EventKeyword)
        ));
        
        //var sourceFileName = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(str));;
        //Add Source name attribute
        node = node.AddMembers(GenerateSourceFileNameField());
        node = node.AddMembers(GenerateExecutedLineNumbersField());
        node = node.AddMembers(GenerateLocalVariableGetter(node));

        return node;
    }

    private MemberDeclarationSyntax GenerateSourceFileNameField()
    {
        var sourceFileNameLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(sourceFileName));
        
        // Create a VariableDeclarationSyntax for the field
        var fieldDeclaration = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
            )
            .AddVariables(
                SyntaxFactory.VariableDeclarator("SourceFileName")
                    .WithInitializer(SyntaxFactory.EqualsValueClause(sourceFileNameLiteral))
            )
        ).AddModifiers(
            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
            SyntaxFactory.Token(SyntaxKind.StaticKeyword)
        );
        
        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SyntaxFactory.Identifier("SourceFileName"))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration
                            ).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        )
                    )
                )
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(sourceFileNameLiteral)
            ).AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            ).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));


        return propertyDeclaration;
    }
    
    private MemberDeclarationSyntax GenerateExecutedLineNumbersField()
    {
        var elements = executableLines.Select(v => 
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, 
                    SyntaxFactory.Literal(v)))
            .ToArray();

        var typeSpecifier = SyntaxFactory.ArrayType(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
            SyntaxFactory.List(new[]
            {
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SeparatedList<ExpressionSyntax>(new[]
                    {
                        SyntaxFactory.OmittedArraySizeExpression()
                    })
                    //SyntaxFactory.SingletonList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression())
                )
            })
        );

        /*
        var arrayCreation = SyntaxFactory.ArrayCreationExpression(
            typeSpecifier,
            SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList<ExpressionSyntax>(elements))
        );*/

        var arrayInitializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
            SyntaxFactory.SeparatedList<ExpressionSyntax>(elements));
        
        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
            typeSpecifier,
            SyntaxFactory.Identifier("ExecutedSourceLines"))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration
                        ).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    )
                )
            )
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(arrayInitializer)
            ).AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            ).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));


        return propertyDeclaration;
    }

    private MemberDeclarationSyntax GenerateLocalVariableGetter(ClassDeclarationSyntax contextClass)
    {
        var fields = contextClass.Members
            .Where(BuilderAnnotations.IsDmlVariableField)
            .Cast<FieldDeclarationSyntax>()
            .Select(x =>
                $"{{\"{BuilderAnnotations.LookupDmlVariableField(x)}\", {typeof(VarEnvObjectReference).FullName}.CreateImmutable(this.{x.Declaration.Variables.Single().Identifier.Text})}}"
            )
            .ToList();

        //String.Join(',', fields);
            /*.ToDictionary(
                x => BuilderAnnotations.LookupDmlVariableField(x),
                x => x.Declaration.Variables.Single().Identifier.Text
            );*/
        
        //Just insane trying to do this in pure Rosyln
        
        var w = CSharpSyntaxTree.ParseText(@"
            public System.Collections.Generic.Dictionary<string, " + typeof(EnvObjectReference).FullName + @"> ImmutableLocalScope {
                get
                {
                    return new System.Collections.Generic.Dictionary<string, " + typeof(EnvObjectReference).FullName + @">() {
                        " + String.Join(',', fields) + @"
                    };
                }
            }
        ").GetCompilationUnitRoot();

        return w.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single();
    }
}