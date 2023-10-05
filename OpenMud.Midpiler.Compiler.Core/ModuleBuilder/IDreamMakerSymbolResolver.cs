using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public struct ModuleMethodDeclarationKey
{
    public readonly int DeclarationOrder;
    public readonly string Name;

    public ModuleMethodDeclarationKey(int declarationOrder, string name)
    {
        DeclarationOrder = declarationOrder;
        Name = name;
    }

    public override bool Equals(object? obj)
    {
        return obj is ModuleMethodDeclarationKey key &&
               DeclarationOrder == key.DeclarationOrder &&
               Name == key.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DeclarationOrder, Name);
    }
}

public interface IDreamMakerSymbolResolver
{
    public IEnumerable<ModuleMethodDeclarationKey> MethodDeclarations { get; }

    void DefineFieldInitializer(string fullPath, ExpressionSyntax initializer, bool replaceExisting = true);
    TypeSyntax ResolvePathType(string fullPath);
    ExpressionSyntax ResolveGlobal(string fullPath);

    void DefineClassMethod(string fullName, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> value,
        int definitionOrder);

    void DefineClassField(string fullName, string typeHint, Func<FieldDeclarationSyntax, FieldDeclarationSyntax> value);

    void DefineMethodConfiguration(string key, Func<AttributeSyntax, AttributeSyntax> decl, int declarationOrder,
        bool replaceExisting = true);

    string DefineSupportMethod(string baseClass, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> value);
    ClassDeclarationSyntax Touch(string parent);
}