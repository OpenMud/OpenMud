using Antlr4.Runtime;
using OpenMud.Mudpiler.Compiler.Core.GrammarSupport;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Weavify.Classifier.Collection;
using System.Text.RegularExpressions;
using static OpenMud.Mudpiler.Compiler.Core.MsBuildDmlCompiler;
using static System.Net.Mime.MediaTypeNames;

internal class Program
{
    private static void ApplyFile(string rootDirectory, UnitCollectionModule unitModule, string fullFilePath)
    {
        unitModule.FileName = fullFilePath;

        var preprocessorCtx = new DmePreprocessContext(rootDirectory, resourceNotFoundHandler: IgnoreResourceNotFoundHandler);
        var sourceFile = preprocessorCtx.PreprocessFile(fullFilePath, EnvironmentConstants.BUILD_MACROS).AsPlainText(false);
        var inputStream = new AntlrInputStream(sourceFile);
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
        r.Visit(unitModule);
    }

    private static void IgnoreResourceNotFoundHandler(string name)
    {
    }

    private static IEnumerable<string> SearchFiles(string directoryPath, string targetExtension)
    {
        if (Directory.Exists(directoryPath))
        {
            // Search for files with the specified extension in the directory
            string[] files = Directory.GetFiles(directoryPath, $"*{targetExtension}", SearchOption.AllDirectories);

            foreach (string file in files)
                yield return file;
        }
    }

    private static void Main(string[] args)
    {
        var rootDir = "C:\\Users\\jerem\\OneDrive\\Desktop\\goonstation-teardown\\game-original";
        //Use DME File...
        //var files = SearchFiles(rootDir, ".dm").ToList();
        var dmeFile = Directory.GetFiles(rootDir).Where(x => x.EndsWith(".dme")).Single();
        var files = Regex
            .Matches(File.ReadAllText(dmeFile), @"#include \""([^\""]+)""")
            .Select(x => x.Groups[1].Value)
            .Where(x => x.EndsWith(".dm")).ToList();
        files = files.Select(f => Path.Join(rootDir, f)).ToList();

        var unitModule = new UnitCollectionModule();
        for (var i = 0; i < files.Count; i++)
        {
            var percentage = (i + 1) / (float)files.Count;
            var friendlyName = Path.GetRelativePath(rootDir, files[i]);
            Console.WriteLine($"Processing file ({percentage:0.0%}) {i + 1} / {files.Count}: {friendlyName}");

            ApplyFile(rootDir, unitModule, files[i]);
        }

        return;

    }
}