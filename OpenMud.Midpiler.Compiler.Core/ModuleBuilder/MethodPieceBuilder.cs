using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public delegate MethodDeclarationSyntax MethodBodyPieceBuilder(IDreamMakerSymbolResolver resolver,
    MethodDeclarationSyntax src);

public class MethodPieceBuilder : IModulePieceBuilder
{
    private readonly int definitionOrder;
    private readonly string fullName;
    private readonly MethodBodyPieceBuilder method;

    //If parent is null, use the root node of the subject tree element (which does not necessarily translate to the root node
    //of the entire tree...)
    public MethodPieceBuilder(MethodBodyPieceBuilder method, string fullName, int definitionOrder)
    {
        if (fullName == null || fullName.Length == 0)
            throw new Exception("Must include at least the name of the method...");

        this.fullName = fullName;
        this.method = method;
        this.definitionOrder = definitionOrder;
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        //resolver.DefineClass(this.parent, c => c.AddMembers(new MemberDeclarationSyntax[] {this.method(resolver)}));
        resolver.DefineClassMethod(fullName, m => method(resolver, m), definitionOrder);
    }
}