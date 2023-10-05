using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;

internal class InheritanceGraphRewriter : CSharpSyntaxRewriter
{
    private readonly Func<string, ClassDeclarationSyntax> lookupCls;
    private readonly Func<ClassDeclarationSyntax, string> lookupName;
    private readonly Func<string, string> lookupPrimitive;

    public InheritanceGraphRewriter(Func<string, ClassDeclarationSyntax> lookupCls,
        Func<string, string> lookupPrimitive, Func<ClassDeclarationSyntax, string> lookupName)
    {
        this.lookupCls = lookupCls;
        this.lookupName = lookupName;
        this.lookupPrimitive = lookupPrimitive;
    }

    private ClassDeclarationSyntax ResolvePrimitiveBaseClass(string basename, ClassDeclarationSyntax cls)
    {
        if (!BuiltinTypes.IsPrimitiveClass(basename))
            basename = "datum";

        return cls.WithBaseList(
            SyntaxFactory.BaseList(
                SyntaxFactory.SeparatedList<BaseTypeSyntax>(new[]
                    {
                        SyntaxFactory.SimpleBaseType(
                            BuiltinTypes.ResolveTypeSyntax(basename)
                        )
                    }
                )
            )
        );
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax cls)
    {
        if (!BuilderAnnotations.IsDmlDatumAtomicClass(cls))
            return cls;

        var name = lookupName(cls);
        var inheritanceGraph = DmlPath.ResolveInheritancePath(name);

        string clsName;

        if (BuiltinTypes.IsPrimitiveClass(name))
        {
            var originPrimitive = lookupPrimitive(name);

            if (originPrimitive == cls.Identifier.Text)
                return ResolvePrimitiveBaseClass(name, cls);
            clsName = lookupCls(originPrimitive).Identifier.Text;
        }
        else
        {
            var parentName = "/" + string.Join('/', inheritanceGraph);
            clsName = lookupCls(parentName).Identifier.Text;
        }

        var r = base.VisitClassDeclaration(cls.WithBaseList(
            SyntaxFactory.BaseList(
                SyntaxFactory.SeparatedList<BaseTypeSyntax>(new[]
                    {
                        SyntaxFactory.SimpleBaseType(
                            SyntaxFactory.ParseTypeName(clsName)
                        )
                    }
                )
            )
        ));

        return r;
    }
}