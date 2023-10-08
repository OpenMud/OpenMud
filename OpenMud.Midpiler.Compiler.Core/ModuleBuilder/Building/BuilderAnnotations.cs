using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;

public static class BuilderAnnotations
{
    public static readonly SyntaxAnnotation CallBaseAnnotation = new();
    public static readonly SyntaxAnnotation CallSelfAnnotation = new();
    public static readonly SyntaxAnnotation ProcClassMethod = new();
    public static readonly SyntaxAnnotation ImplicitReturnAssignment = new();
    public static readonly SyntaxAnnotation DontWrapAnnotation = new();
    public static readonly SyntaxAnnotation DmlCodeVariableAnnotation = new();
    public static readonly SyntaxAnnotation SkipScopeResolution = new();
    public static readonly SyntaxAnnotation UnmanagedReturnValue = new();
    public static readonly SyntaxAnnotation DmlInvoke = new();
    public static readonly SyntaxAnnotation DmlImmediateEvaluateMethod = new();
    public static readonly SyntaxAnnotation DmlNativeDeferred = new();

    public static SyntaxAnnotation MapSourceFile(string filename, int lineNumber)
    {
        return new SyntaxAnnotation("sourceMap", $"{lineNumber}:{filename}");
    }
    public static bool GetSourceMap(SyntaxNode n, out string filename, out int lineNumber)
    {
        filename = "";
        lineNumber = 0;

        if (!n.HasAnnotations("sourceMap"))
            return false;

        var str = n.GetAnnotations("sourceMap").First().Data;

        lineNumber = int.Parse(str.Substring(0, str.IndexOf(":")));
        filename = str.Substring(str.IndexOf(":") + 1);

        return true;
    }

    public static SyntaxAnnotation CreateProcNameAnnotation(string name)
    {
        return new SyntaxAnnotation("dmlProcName", name);
    }

    public static SyntaxAnnotation CreateManagedArgsAnnotation(int managedStartIndex)
    {
        return new SyntaxAnnotation("dmlManagedArgs", managedStartIndex.ToString());
    }

    public static int GetManagedArgsAnnotation(SyntaxNode n)
    {
        return int.Parse(n.GetAnnotations("dmlManagedArgs").Select(n => n.Data).First());
    }

    public static bool HasManagedArgsAnnotation(SyntaxNode n)
    {
        return n.HasAnnotations("dmlManagedArgs");
    }

    public static bool HasProcNameAnnotation(SyntaxNode n)
    {
        return n.HasAnnotations("dmlProcName");
    }

    public static string GetProcNameAnnotation(SyntaxNode n)
    {
        return n.GetAnnotations("dmlProcName").Select(n => n.Data).First();
    }

    public static SyntaxAnnotation CreateTypeHints(string typeHint)
    {
        return new SyntaxAnnotation("varTypeHint", DmlPath.RootClassName(typeHint));
    }

    public static SyntaxAnnotation CreateIsTypeWithoutType(string testVar)
    {
        return new SyntaxAnnotation("varIsType", testVar);
    }

    public static SyntaxAnnotation CreateInferNewType(string testVar)
    {
        return new SyntaxAnnotation("newTypeTarget", testVar);
    }

    public static SyntaxAnnotation CreateClassPathAnnotation(string className)
    {
        return new SyntaxAnnotation("dmlClassName", className);
    }

    public static SyntaxAnnotation CreateProcClassPathAnnotation(string className)
    {
        return new SyntaxAnnotation("dmlProcClass", className);
    }

    public static bool HasDmlProcClassAnnotation(SyntaxNode n)
    {
        return n.HasAnnotations("dmlProcClass");
    }

    public static bool IsDmlDatumAtomicClass(SyntaxNode n)
    {
        return n.HasAnnotations("dmlClassName");
    }

    public static string GetDmlProcClassAnnotation(SyntaxNode n)
    {
        return n.GetAnnotations("dmlProcClass").Select(n => n.Data).First();
    }

    public static bool HasDmlClassPath(SyntaxNode n, string classPath)
    {
        return n.HasAnnotations("dmlClassName") && n.GetAnnotations("dmlClassName").First().Data == classPath;
    }

    public static bool ExtractTypeHintAnnotation(SyntaxNode n, out string? typeHint)
    {
        typeHint = n.GetAnnotations("varTypeHint").Select(n => n.Data).FirstOrDefault();

        return typeHint != null;
    }

    public static bool HasIsTypeAnnotationWithoutType(SyntaxNode n)
    {
        return n.HasAnnotations("varIsType");
    }

    public static string GetIsTypeTarget(SyntaxNode n)
    {
        return n.GetAnnotations("varIsType").Single().Data;
    }


    public static bool HasInferNewTypeTarget(SyntaxNode n)
    {
        return n.HasAnnotations("newTypeTarget");
    }

    public static string GetInferNewTypeTarget(SyntaxNode n)
    {
        return n.GetAnnotations("newTypeTarget").Single().Data;
    }

    public static string ExtractTypeHintAnnotationOrDefault(SyntaxNode n, string def = "")
    {
        if (!ExtractTypeHintAnnotation(n, out var typeHint))
            return def;

        return typeHint;
    }

    public static bool SkipGlobalResolution(SyntaxNode name)
    {
        return !name.HasAnnotation(DmlCodeVariableAnnotation) || name.HasAnnotation(SkipScopeResolution);
    }

    internal static bool IsRootGlobal(ClassDeclarationSyntax node)
    {
        return HasDmlClassPath(node, "/");
    }
}