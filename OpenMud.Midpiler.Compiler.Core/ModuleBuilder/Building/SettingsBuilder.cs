using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;

public static class SettingsBuilder
{
    private static readonly Dictionary<string, string> AttributeMapping = new()
    {
        { "name", typeof(VerbName).FullName },
        { "category", typeof(VerbCategory).FullName },
        { "src", typeof(VerbSrc).FullName },
        { "desc", typeof(VerbDescription).FullName },
        { "background", typeof(BackgroundProcessing).FullName },
        { "hidden", typeof(Hidden).FullName },
        { "instant", typeof(Instant).FullName },
        { "popup_menu", typeof(PopupMenu).FullName }
    };

    public static AttributeSyntax CreateVerb()
    {
        return SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(typeof(Verb).FullName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(
                )
            )
        );
    }

    public static bool IsVerbAttribute(AttributeSyntax a)
    {
        return a.Name.IsEquivalentTo(SyntaxFactory.ParseName(typeof(Verb).FullName));
    }

    public static AttributeSyntax CreateAttribute(string setting)
    {
        if (!AttributeMapping.TryGetValue(setting, out var attrName))
            throw new Exception("Unknown setting field: " + setting);

        return SyntaxFactory.Attribute(SyntaxFactory.ParseName(attrName));
    }

    internal static bool IsAttribute(string setting, NameSyntax name)
    {
        if (!AttributeMapping.TryGetValue(setting, out var attrName))
            throw new Exception("Unknown setting field: " + setting);

        return name.IsEquivalentTo(SyntaxFactory.ParseName(attrName));
    }
}