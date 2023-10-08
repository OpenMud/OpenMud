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
}