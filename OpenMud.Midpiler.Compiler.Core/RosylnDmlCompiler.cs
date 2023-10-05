using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using OpenMud.Mudpiler.Compiler.Core.GrammarSupport;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Compiler.Core;

public static class RosylnDmlCompiler
{
    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var allReferences = new List<MetadataReference>();
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        allReferences.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));
        allReferences.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")));
        allReferences.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")));
        allReferences.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
        allReferences.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Microsoft.CSharp.dll")));

        allReferences.Add(MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location));
        allReferences.Add(MetadataReference.CreateFromFile(typeof(Atom).Assembly.Location));
        allReferences.Add(MetadataReference.CreateFromFile(typeof(DmlPath).Assembly.Location));

        var staticDependencies = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "./CompileBins"),
            "*.dll", new EnumerationOptions { RecurseSubdirectories = true });

        foreach (var sd in staticDependencies)
            allReferences.Add(MetadataReference.CreateFromFile(sd));

        return allReferences;
    }

    private static string CompileIntermediate(string cSharpIntermediate, string? destAssemblyName = null)
    {
        var assemblyFileName = destAssemblyName;

        if (assemblyFileName == null)
            assemblyFileName = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid() + ".dmltest.asm");

        var compilation =
            CreateCompilation(CSharpSyntaxTree.ParseText(cSharpIntermediate), "intermediate" + Guid.NewGuid());
        compilation = compilation.AddReferences(GetMetadataReferences());


        var r = compilation.Emit(assemblyFileName);

        if (!r.Success)
            throw new Exception(
                string.Join("\n",
                    r.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(x => $"{x.Location.GetLineSpan().StartLinePosition}: {x.GetMessage()}")));


        return assemblyFileName;
    }

    private static CSharpCompilation CreateCompilation(SyntaxTree tree, string name)
    {
        return CSharpCompilation
            .Create(name, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
            .AddSyntaxTrees(tree);
    }

    public static string Compile(string text, string? destAssemblyName = null)
    {
        var inputStream = new AntlrInputStream(text);
        var lexer = new LexerWithIndentInjector(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var errorListener = new ErrorListener();

        lexer.AddErrorListener(errorListener);

        var parser = new DmlParser(commonTokenStream);
        parser.AddErrorListener(errorListener);

        var ctx = parser.dml_module();
        var visitor = new BasicModuleVisitor();

        var allErrors = lexer.GetErrorMessages().Concat(errorListener.Errors).ToList();

        if (allErrors.Any())
            throw new Exception(string.Join("\n", allErrors));

        var r = visitor.Visit(ctx);
        CSharpModule module = new();
        r.Visit(module);

        var compilationUnit = module.CreateCompilationUnit();

        var cw = new AdhocWorkspace();
        cw.Options.WithChangedOption(CSharpFormattingOptions.IndentBraces, true);
        var formattedCode = Formatter.Format(compilationUnit, cw);
        Console.WriteLine(formattedCode);

        return CompileIntermediate(formattedCode.ToFullString(), destAssemblyName);
    }

    public class ErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        public readonly List<string> Errors = new();

        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] int offendingSymbol, int line,
            int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            Errors.Add(e.Message);
        }

        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line,
            int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            Errors.Add($"{line}:{charPositionInLine} {offendingSymbol}");
        }
    }
}