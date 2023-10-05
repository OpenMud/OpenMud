using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;

public delegate FieldDeclarationSyntax FieldBodyPieceBuilder(IDreamMakerSymbolResolver resolver,
    FieldDeclarationSyntax fld);

public class FieldPieceBuilder : IModulePieceBuilder
{
    private readonly FieldBodyPieceBuilder field;
    private readonly string fullName;
    private readonly string typeHint;

    public FieldPieceBuilder(FieldBodyPieceBuilder field, string typeHint, string fullName)
    {
        if (fullName == null || fullName.Length == 0)
            throw new Exception("Must include at least the name of the Field...");

        this.fullName = fullName;
        this.field = field;
        this.typeHint = typeHint;
    }

    public void Visit(IDreamMakerSymbolResolver resolver)
    {
        resolver.DefineClassField(fullName, typeHint, fld => field(resolver, fld));
    }
}