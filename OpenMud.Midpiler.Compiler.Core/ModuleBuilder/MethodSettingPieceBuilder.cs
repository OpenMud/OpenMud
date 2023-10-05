using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public delegate Func<AttributeSyntax, AttributeSyntax> SettingPieceBodyBuilder(IDreamMakerSymbolResolver resolver);

public class MethodSettingPieceBuilder : IModulePieceBuilder
{
    private readonly string key;
    private readonly int methodDeclarationOrder;
    private readonly SettingPieceBodyBuilder value;

    public MethodSettingPieceBuilder(string key, int methodDeclarationOrder, SettingPieceBodyBuilder value)
    {
        this.key = key;
        this.value = value;
        this.methodDeclarationOrder = methodDeclarationOrder;
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        resolver.DefineMethodConfiguration(key, value(resolver), methodDeclarationOrder);
    }
}