using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Weavify.Classifier.Collection
{
    public enum ModuleForm
    {
        Class,
        GlobalField,
        GlobalProc
    }

    public class ModuleUnit
    {
        public readonly string FileName;
        public readonly string PieceName;
        public readonly ModuleForm Form;

        public ModuleUnit(string fileName, string pieceName, ModuleForm form)
        {
            this.FileName = fileName;
            this.PieceName = pieceName;
            this.Form = form;
        }

        public override bool Equals(object? obj)
        {
            return obj is ModuleUnit unit &&
                   FileName == unit.FileName &&
                   PieceName == unit.PieceName &&
                   Form == unit.Form;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileName, PieceName, Form);
        }
    }

    internal class UnitCollectionModule : IDreamMakerSymbolResolver
    {
        public string FileName { get; set; } = "";

        public IEnumerable<ModuleMethodDeclarationKey> MethodDeclarations => throw new NotImplementedException();
        public ISet<ModuleUnit> Units { get; } = new HashSet<ModuleUnit>();

        public void DefineClassField(string fullPath, string typeHint, Func<FieldDeclarationSyntax, FieldDeclarationSyntax> value)
        {
            DmlPath.ParseNamespacePath(fullPath, out var effectiveFullPath, out var fieldName, true);

            if (fieldName.Length == 0)
                throw new ArgumentException("Invalid field name.");

            //Define all of the base classes..
            var baseClassName = DmlPath.BuildQualifiedDeclarationName(effectiveFullPath);
            var absoluteName = DmlPath.BuildQualifiedDeclarationName(baseClassName, fieldName);
            var fullFieldName = DmlPath.BuildQualifiedDeclarationName(effectiveFullPath, fieldName!);

            Touch(baseClassName);

            if (baseClassName == DmlPath.GLOBAL_PATH)
                Units.Add(new ModuleUnit(FileName, fullFieldName, ModuleForm.GlobalField));
        }

        public void DefineClassMethod(string fullPath, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> value, int definitionOrder)
        {
            DmlPath.ParseNamespacePath(fullPath, out var effectiveClassNameFullPath, out var methodName, true);

            var effectiveFullPath = DmlPath.BuildQualifiedDeclarationName(effectiveClassNameFullPath);
            var fullMethodName = DmlPath.BuildQualifiedDeclarationName(effectiveFullPath, methodName!);

            if(effectiveClassNameFullPath == DmlPath.GLOBAL_PATH)
                Units.Add(new ModuleUnit(FileName, fullMethodName, ModuleForm.GlobalProc));
        }

        public void DefineFieldInitializer(string fullPath, ExpressionSyntax initializer, bool replaceExisting = true)
        {
        }

        public void DefineMethodConfiguration(string key, Func<AttributeSyntax, AttributeSyntax> decl, int declarationOrder, bool replaceExisting = true)
        {
        }

        public string DefineSupportMethod(string baseClass, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> value)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax ResolveGlobal(string fullPath)
        {
            throw new NotImplementedException();
        }

        public TypeSyntax ResolvePathType(string fullPath)
        {
            throw new NotImplementedException();
        }

        public void Touch(string fullPath)
        {
            var pathName = DmlPath.BuildQualifiedDeclarationName(fullPath);

            if (pathName.Length == 0)
                throw new ArgumentException("Invalid class name path.");

            var baseClassName = DmlPath.ResolveParentClass(pathName);
            if (baseClassName != null)
                Touch(baseClassName);

            Units.Add(new ModuleUnit(FileName, pathName, ModuleForm.Class));
        }
    }
}
