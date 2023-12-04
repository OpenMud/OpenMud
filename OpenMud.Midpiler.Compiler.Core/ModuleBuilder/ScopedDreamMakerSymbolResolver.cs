using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public class ScopedDreamMakerSymbolResolver : IDreamMakerSymbolResolver
{
    private readonly string basePath;
    private readonly IDreamMakerSymbolResolver inner;

    public ScopedDreamMakerSymbolResolver(string basePath, IDreamMakerSymbolResolver inner)
    {
        this.basePath = basePath;
        this.inner = inner;
    }

    public IEnumerable<ModuleMethodDeclarationKey> MethodDeclarations => inner.MethodDeclarations;

    public ExpressionSyntax ResolveGlobal(string fullPath)
    {
        return inner.ResolveGlobal(fullPath);
    }

    public TypeSyntax ResolvePathType(string fullPath)
    {
        return inner.ResolvePathType(fullPath);
    }

    public void DefineFieldInitializer(string fullPath, ExpressionSyntax initializer, bool replaceExisting = true)
    {
        inner.DefineFieldInitializer(DmlPath.Concat(basePath, fullPath), initializer, replaceExisting);
    }

    public void DefineClassMethod(string fullPath, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> decl,
        int definitionORder)
    {
        inner.DefineClassMethod(DmlPath.Concat(basePath, fullPath), decl, definitionORder);
    }

    public void DefineClassField(string fullPath, string typeName,
        Func<FieldDeclarationSyntax, FieldDeclarationSyntax> decl)
    {
        inner.DefineClassField(DmlPath.Concat(basePath, fullPath), typeName, decl);
    }

    public void DefineMethodConfiguration(string key, Func<AttributeSyntax, AttributeSyntax> value,
        int declarationOrder, bool replaceExisting = true)
    {
        inner.DefineMethodConfiguration(DmlPath.Concat(basePath, key), value, declarationOrder, replaceExisting);
    }

    public void Touch(string path)
    {
        inner.Touch(DmlPath.Concat(basePath, path));
    }

    public string DefineSupportMethod(string baseClass, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> value)
    {
        return inner.DefineSupportMethod(DmlPath.Concat(basePath, baseClass), value);
    }
}