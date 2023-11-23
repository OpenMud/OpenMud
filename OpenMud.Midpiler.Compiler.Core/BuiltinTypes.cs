using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Compiler.Core;

public static class BuiltinTypes
{
    private static readonly Dictionary<string, TypeSyntax> BasicTypes = new()
    {
        { "num", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)) },
        { "text", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)) },
        { "message", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)) },
        { "anything", SyntaxFactory.ParseTypeName("dynamic") }
    };


    private static readonly Dictionary<string, Type> PrimitiveBaseClasses = new()
    {
        { "/atom", typeof(Atom) },
        { "/GLOBAL", typeof(Global) },
        { "/world", typeof(GameWorld) },
        { "/datum", typeof(Datum) },
        { "/client", typeof(GameClient) }
    };

    public static TypeSyntax ResolveTypeSyntax(string name)
    {
        var aliasedName = DmlPath.BuildQualifiedDeclarationName(name);

        if (BasicTypes.TryGetValue(aliasedName, out var typeSyntax))
            return typeSyntax;

        if (PrimitiveBaseClasses.TryGetValue(aliasedName, out var type))
            return SyntaxFactory.ParseTypeName(type.FullName);

        throw new Exception("Unknown primitive type.");
    }

    public static Type ResolveType(string name)
    {
        name = DmlPath.BuildQualifiedDeclarationName(name);

        if (PrimitiveBaseClasses.TryGetValue(name, out var type)) return type;

        throw new Exception("Unknown primitive type.");
    }

    public static TypeSyntax ResolveGenericType()
    {
        return SyntaxFactory.ParseTypeName("dynamic");
    }
}
