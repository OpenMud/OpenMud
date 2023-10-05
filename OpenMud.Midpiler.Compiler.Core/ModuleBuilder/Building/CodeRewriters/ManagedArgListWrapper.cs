using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class ManagedArgListWrapper : CSharpSyntaxRewriter
{
    private int blockDepth = 0;

    private ArgumentListSyntax GenerateArgListWrapper(ArgumentListSyntax originate, int managedArgsStartIdx)
    {
        //First argument is the method name.
        var positional = originate
            .Arguments
            .Skip(managedArgsStartIdx)
            .Where(a => a.NameColon == null);

        var namedArgs = originate
            .Arguments
            .Skip(managedArgsStartIdx)
            .Where(x => x.NameColon != null)
            .ToDictionary(
                x => x.NameColon.Name.Identifier.Text,
                x => x.WithNameColon(null)
            );

        ArgumentSyntax createArg(string? name, ArgumentSyntax inner)
        {
            List<ArgumentSyntax> arguments = new();

            if (name != null)
                arguments.Add(SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(name)))
                    .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation)
                );

            arguments.Add(inner);

            var argObj =
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseName(typeof(ProcArgument).FullName),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            arguments
                        )
                    ),
                    null
                );

            return SyntaxFactory.Argument(argObj);
        }

        ;

        var r = SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.ParseName(typeof(ProcArgumentList).FullName),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    positional
                        .Select(p => createArg(null, p))
                        .Concat(
                            namedArgs
                                .Select(
                                    p => createArg(p.Key, p.Value)
                                )
                        )
                )
            ),
            null
        );

        return SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(
                originate.Arguments.Take(managedArgsStartIdx).Append(
                    SyntaxFactory.Argument(r)
                )
            )
        );
    }

    public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
    {
        var r = base.VisitArgumentList(node);

        if (BuilderAnnotations.HasManagedArgsAnnotation(r))
            r = GenerateArgListWrapper((ArgumentListSyntax)r, BuilderAnnotations.GetManagedArgsAnnotation(r));

        return r;
    }
}