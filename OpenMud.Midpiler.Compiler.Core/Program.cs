using System.Runtime.InteropServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using OpenMud.Mudpiler.Compiler.Core.GrammarSupport;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.Compiler.DmlGrammar;

namespace OpenMud.Mudpiler.Compiler.Core;

public class Program
{
    public static void Main(string[] args)
    {
        NewMain(args);
    }

    private static void NewMain(string[] args)
    {
        if (args.Length != 1 || !Directory.Exists(args[0]))
        {
            Console.Error.WriteLine("Error, first argument must be a directory to the source dm code.");
            return;
        }

        var files = Directory.GetFiles(args[0], "*.dm", new EnumerationOptions { RecurseSubdirectories = true });
        var binDir = Path.Join(args[0], ".\\bin");

        Directory.CreateDirectory(binDir);

        var concatSources = string.Join("\n", files.Select(f => File.ReadAllText(f)));

        File.WriteAllText(Path.Join(binDir, "srcGlob.txt"), concatSources);

        MsBuildDmlCompiler.Compile(concatSources, Path.Join(binDir, "DmlProject.dll"));
    }

    private static void MainOld(string[] args)
    {
        //try
        //{
        /*
        string input = "";
        var text = new StringBuilder();

        // to type the EOF character and end the input: use CTRL+D, then press <enter>
        while ((input = Console.ReadLine()) != null)
            text.AppendLine(input);
        */
        var prefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Users\jerem\OneDrive\Documents\dml_transpiler\dreammaker_net\lab\"
            : "/mnt/c/Users/jerem/OneDrive/Documents/dml_transpiler/dreammaker_net/lab/";
        var text = File.ReadAllText($"{prefix}test_input.dm");
        var inputStream = new AntlrInputStream(text);
        var speakLexer = new LexerWithIndentInjector(inputStream);
        var commonTokenStream = new CommonTokenStream(speakLexer);
        var speakParser = new DmlParser(commonTokenStream);

        var chatContext = speakParser.dml_module();
        var visitor = new BasicModuleVisitor();

        ITree pt = chatContext; //speakParser.dml_module();

        if (speakLexer.GetErrorMessages().Any())
        {
            foreach (var e in speakLexer.GetErrorMessages())
                Console.WriteLine(e);

            Environment.Exit(1);
        }

        var r = visitor.Visit(chatContext);
        CSharpModule module = new();
        r.Visit(module);
        //Console.WriteLine(module.CreateCompilationUnit().NormalizeWhiteSpace().ToFullString());

        var cw = new AdhocWorkspace();
        cw.Options.WithChangedOption(CSharpFormattingOptions.IndentBraces, true);
        var formattedCode = Formatter.Format(module.CreateCompilationUnit(), cw);
        Console.WriteLine(formattedCode);
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Error: " + ex);                
        //}
    }
}