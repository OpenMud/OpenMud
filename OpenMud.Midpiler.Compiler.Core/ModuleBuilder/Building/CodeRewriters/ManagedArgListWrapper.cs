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
            .Where(a => !a.HasAnnotation(BuilderAnnotations.NamedArgumentTuple));

        var namedArgs = originate
            .Arguments
            .Skip(managedArgsStartIdx)
            .Where(x => x.HasAnnotation(BuilderAnnotations.NamedArgumentTuple))
            .Select(x => (TupleExpressionSyntax)x.Expression)
            .ToDictionary(
                x => x.Arguments[0].Expression,
                x => SyntaxFactory.Argument(x.Arguments[1].Expression)
            );

        ArgumentSyntax createArg(ExpressionSyntax? name, ArgumentSyntax inner)
        {
            List<ArgumentSyntax> arguments = new();

            if (name != null)
                arguments.Add(SyntaxFactory.Argument(name)
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
        else if (r.GetAnnotatedNodes(BuilderAnnotations.NamedArgumentTuple).Any())
            throw new Exception("Cannot have named arguments in a non-managed argument list.");

        return r;
    }
}