using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

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
        { "/datum", typeof(Datum) }
    };

    private static readonly Dictionary<string, string> TypeAliases = new()
    {
        { "/mob", "/atom/movable/mob" },
        { "/obj", "/atom/movable/obj" },
        { "/turf", "/atom/turf" },
        { "/area", "/atom/area" },
        { "/movable", "/atom/movable" }
    };

    public static IEnumerable<string> PrimitiveClassNames => PrimitiveBaseClasses.Keys.Select(DmlPath.RootClassName);

    public static IEnumerable<string> PredefinedTypes =>
        PrimitiveClassNames.Concat(TypeAliases.Keys).Select(DmlPath.RootClassName);

    public static IImmutableDictionary<string, string> TypeAliaseMapping => TypeAliases.ToImmutableDictionary();

    public static IEnumerable<string> EnumerateProcNameAliasesOf(string procName)
    {
        List<string> ProduceProcAliases(string name)
        {
            var comps = DmlPath.Split(name).ToList();

            var tail = comps.Last();

            comps.RemoveAt(comps.Count - 1);

            var reserved = new[] { "verb", "proc" };

            if (comps.Any() && reserved.Contains(comps.Last()))
                comps.RemoveAt(comps.Count - 1);

            return new[]
                {
                    comps.Append(tail),
                    comps.Append("verb").Append(tail),
                    comps.Append("proc").Append(tail)
                }
                .Select(p => DmlPath.RootClassName(string.Join("/", p)))
                .ToList();
        }

        var simple = EnumerateAliasesOf(procName);

        return simple.Concat(
                simple.SelectMany(ProduceProcAliases)
            )
            .Distinct()
            .ToList();
    }

    public static IEnumerable<string> EnumerateAliasesOf(string className)
    {
        var baseName = ResolveClassAlias(className);

        yield return baseName;

        foreach (var (k, v) in TypeAliases)
            if (baseName.StartsWith(v))
                yield return DmlPath.NormalizeClassName(k + "/" + baseName.Substring(v.Length));
    }

    public static string ResolveClassAlias(string className)
    {
        foreach (var a in TypeAliaseMapping)
            if (className.StartsWith(a.Key))
                return DmlPath.NormalizeClassName(a.Value + "/" + className.Substring(a.Key.Length));

        return className;
    }

    public static bool IsPrimitiveClass(string name)
    {
        var aliasedName = DmlPath.NormalizeClassName(DmlPath.RootClassName(ResolveClassAlias(name)));
        return PrimitiveBaseClasses.ContainsKey(aliasedName);
    }

    public static TypeSyntax ResolveTypeSyntax(string name)
    {
        var aliasedName = DmlPath.NormalizeClassName(DmlPath.RootClassName(ResolveClassAlias(name)));

        if (BasicTypes.TryGetValue(aliasedName, out var typeSyntax))
            return typeSyntax;

        if (PrimitiveBaseClasses.TryGetValue(aliasedName, out var type))
            return SyntaxFactory.ParseTypeName(type.FullName);

        throw new Exception("Unknown primitive type.");
    }

    public static Type ResolveType(string name)
    {
        name = DmlPath.NormalizeClassName(DmlPath.RootClassName(ResolveClassAlias(name)));

        if (PrimitiveBaseClasses.TryGetValue(name, out var type)) return type;

        throw new Exception("Unknown primitive type.");
    }

    public static TypeSyntax ResolveGenericType()
    {
        return SyntaxFactory.ParseTypeName("dynamic");
    }
}