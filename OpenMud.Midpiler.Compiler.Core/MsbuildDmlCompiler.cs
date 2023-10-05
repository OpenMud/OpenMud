using System.Diagnostics;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using OpenMud.Mudpiler.Compiler.Core.GrammarSupport;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.Compiler.DmlGrammar;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Compiler.Core;

public class MsBuildDmlCompiler
{
    private static string CreateProject(string cSharpIntermediate, string projectDirectory)
    {
        var csProjFile = Path.Combine(projectDirectory, "./DmlProject.csproj");
        var srcfileName = Path.Combine(projectDirectory, "./Dml.cs");
        var weaverFileName = Path.Combine(projectDirectory, "FodyWeavers.xml");

        // Create a new project using the ProjectRootElement class
        var projectRootElement = ProjectRootElement.Create();

        projectRootElement.Sdk = "Microsoft.NET.Sdk";
        projectRootElement.ToolsVersion = null;


        var csPropertyGroup = projectRootElement.AddPropertyGroup();
        csPropertyGroup.AddProperty("TargetFramework", "net7.0");
        csPropertyGroup.AddProperty("OutputType", "Library");


        // Add an ItemGroup element for PackageReference
        var packageReferenceItemGroup = projectRootElement.AddItemGroup();

        // Add the first PackageReference
        var packageReference1 = packageReferenceItemGroup.AddItem("PackageReference", "Fody");
        packageReference1.AddMetadata("Version", "6.8.0");
        packageReference1.AddMetadata("PrivateAssets", "All");
        packageReference1.AddMetadata("IncludeAssets",
            "framework; build; native; contentfiles; analyzers; buildtransitive");

        // Add the second PackageReference
        var packageReference2 = packageReferenceItemGroup.AddItem("PackageReference", "InlineIL.Fody");
        packageReference2.AddMetadata("PrivateAssets", "All");
        packageReference2.AddMetadata("Version", "1.7.4");


        // Add another ItemGroup element for Reference
        var referenceItemGroup = projectRootElement.AddItemGroup();

        // Add a Reference item
        var referenceItem = referenceItemGroup.AddItem("Reference",
            typeof(OpenMudEnvironmentRootPlaceholder).Assembly.GetName().Name);
        referenceItem.AddMetadata("HintPath", typeof(Atom).Assembly.Location);

        File.WriteAllText(srcfileName, cSharpIntermediate);
        File.WriteAllText(weaverFileName, @"
<Weavers xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""FodyWeavers.xsd"">
  <InlineIL />
</Weavers>
");

        projectRootElement.Save(csProjFile);

        return csProjFile;
    }

    private static string CompileIntermediate(string cSharpIntermediate, string? destAssemblyName, string msbuildtools,
        bool disposeIntermediate = true)
    {
        if (destAssemblyName == null)
            destAssemblyName = Guid.NewGuid() + ".dmlproject.dll";

        var absoluteAssemblyName = Path.GetFullPath(destAssemblyName);

        var projectDirectory = destAssemblyName + "_project";

        Directory.CreateDirectory(projectDirectory);

        try
        {
            CreateProject(cSharpIntermediate, projectDirectory);
            DoProjectRestore(projectDirectory, msbuildtools);
            var compiledBin = DoProjectBuild(projectDirectory, msbuildtools);

            File.Copy(compiledBin, absoluteAssemblyName, true);

            return absoluteAssemblyName;
        }
        finally
        {
            if (disposeIntermediate)
                Directory.Delete(projectDirectory, true);
        }
    }

    private static string DoProjectBuild(string projectPath, string msbuildtools)
    {
        var binFile = Path.Join(projectPath, ".\\dmlbins\\DmlProject.dll");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{msbuildtools}\\msbuild.dll\" -t:build,FodyTarget -p:OutDir=dmlbins \"{projectPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var buildProc = Process.Start(startInfo);

        var stdOut = buildProc.StandardOutput.ReadToEndAsync();
        var stdErr = buildProc.StandardError.ReadToEndAsync();

        var output = Task.WhenAll(stdOut, stdErr).Result;

        buildProc.WaitForExit();

        Console.WriteLine(output[0]);
        Console.Error.WriteLine(output[1]);

        if (buildProc.ExitCode != 0)
            throw new Exception("Project build error.");

        return binFile;
    }

    private static void DoProjectRestore(string projectPath, string msbuildtools)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{msbuildtools}\\msbuild.dll\" -t:restore \"{projectPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var buildProc = Process.Start(startInfo);

        var stdOut = buildProc.StandardOutput.ReadToEndAsync();
        var stdErr = buildProc.StandardError.ReadToEndAsync();

        var output = Task.WhenAll(stdOut, stdErr).Result;

        buildProc.WaitForExit();

        Console.WriteLine(output[0]);
        Console.Error.WriteLine(output[1]);

        if (buildProc.ExitCode != 0)
            throw new Exception("Project restore error.");
    }

    public static string Compile(string text, string? destAssemblyName = null, bool disposeIntermediate = true)
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

        var sb = new StringBuilder();
        using (var writer = new StringWriter(sb))
        {
            compilationUnit.NormalizeWhitespace().WriteTo(writer);
        }

        var formattedCode = sb.ToString();

        //var formattedCode = Formatter.Format(compilationUnit, cw);

        Console.WriteLine(formattedCode);

        var buildAssembly = MSBuildLocator.QueryVisualStudioInstances().Where(x => x.Version.Major == 7)
            .FirstOrDefault();

        if (buildAssembly == null)
            throw new Exception("Not able to locate a VS Build Instance for .Net 7.0");

        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        return CompileIntermediate(formattedCode, destAssemblyName, buildAssembly.MSBuildPath, disposeIntermediate);
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