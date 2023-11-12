using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Compiler.Project.Project;

public class DmePreprocessContext
{
    private readonly ISet<string> importedMaps;
    private readonly ISet<string> importedStyles;
    private readonly ISet<string> importedInterface;
    private readonly string workingDirectory;

    public DmePreprocessContext(string workingDirectory, ISet<string>? maps = null, ISet<string>? styles = null, ISet<string>? interfaces = null)
    {
        this.workingDirectory = workingDirectory;

        importedMaps = maps ?? new HashSet<string>();
        importedStyles = styles ?? new HashSet<string>();
        importedInterface = interfaces ?? new HashSet<string>();
    }

    public IEnumerable<string> ImportedMaps => importedMaps;

    public IEnumerable<string> ImportedStyles => importedStyles;

    public IEnumerable<string> ImportedInterfaces => importedInterface;

    private IImmutableSourceFileDocument PreprocessContents(string fullFilePath, string resourceBasePath, IEnumerable<string> resourceDirectories, string contents,
        IImmutableDictionary<string, MacroDefinition>? predef,
        out IImmutableDictionary<string, MacroDefinition> resultantMacro)
    {
        return Preprocessor.PreprocessAsDocument(fullFilePath, resourceBasePath, resourceDirectories, contents, ResolveResourceDirectory,
            ProcessImport, out resultantMacro, predef);
    }

    private string ResolveResourceDirectory(List<string> knownFileDirs, string path)
    {
        foreach (var p in knownFileDirs)
        {
            var b = Path.Combine(p, path);
            var actual = Path.Combine(workingDirectory, b);

            if (Path.Exists(actual))
                return b;
        }

        Console.Error.WriteLine("Warning, resource could not be found: " + path);

        return path;
    }

    private (IImmutableDictionary<string, MacroDefinition> macros, IImmutableSourceFileDocument importBody) ProcessImport(
        IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib,
        string fileName)
    {
        fileName = Path.Combine(workingDirectory, fileName);

        if (fileName.ToLower().EndsWith(".dmm"))
        {
            importedMaps.Add(fileName);
            return (dict, new EmptySourceFileDocument());
        }
        else if (fileName.ToLower().EndsWith(".dms"))
        {
            importedStyles.Add(fileName);
            return (dict, new EmptySourceFileDocument());
        }
        else if (fileName.ToLower().EndsWith(".dmf"))
        {
            importedInterface.Add(fileName);
            return (dict, new EmptySourceFileDocument());
        }

        var r = PreprocessFile(fileName, resourceDirectories, dict, out var resultant);
        return (resultant, r);
    }

    public IImmutableSourceFileDocument PreprocessFile(string dmeFile, IEnumerable<string> resourceDirectories,
        IImmutableDictionary<string, MacroDefinition>? predef,
        out IImmutableDictionary<string, MacroDefinition> resultantMacro)
    {
        var rsrcPath = Path.GetDirectoryName(dmeFile) ?? "./";

        dmeFile = Path.GetFullPath(dmeFile);

        return new DmePreprocessContext(Path.GetDirectoryName(dmeFile) ?? workingDirectory, importedMaps, importedStyles, importedInterface)
            .PreprocessContents(dmeFile, rsrcPath, resourceDirectories, File.ReadAllText(dmeFile), predef, out resultantMacro);
    }

    public IImmutableSourceFileDocument PreprocessFile(string dmeFile, IImmutableDictionary<string, MacroDefinition>? predef = null,
        IEnumerable<string> resourceDirectories = null)
    {
        var rsrcPath = Path.Combine(workingDirectory, Path.GetDirectoryName(dmeFile) ?? "./");

        dmeFile = Path.GetFullPath(dmeFile);

        return new DmePreprocessContext(rsrcPath, importedMaps, importedStyles, importedInterface)
            .PreprocessContents(dmeFile, rsrcPath, resourceDirectories ?? Enumerable.Empty<string>(), File.ReadAllText(dmeFile),
                predef, out var _);
    }
}

public class DmeProject
{
    public readonly Assembly Logic;
    public readonly IImmutableDictionary<string, IMudSceneBuilder> Maps;

    public DmeProject(Assembly logic, IImmutableDictionary<string, IMudSceneBuilder> maps)
    {
        Logic = logic;
        Maps = maps;
    }

    public string Preprocess(string dmeFile)
    {
        var rootCtx = new DmePreprocessContext(Path.GetDirectoryName(dmeFile) ?? Directory.GetCurrentDirectory());

        return rootCtx.PreprocessFile(dmeFile).AsPlainText();
    }

    public static DmeProject Load(string binPath, IMudEntityBuilder builder)
    {
        var absEnvPath = Path.GetFullPath(binPath);
        var importedMaps = Directory.GetFiles(absEnvPath, "*.dmm");

        var dmmSceneBuilder = new DmmSceneBuilderFactory(builder);

        var asmBinPath = Path.Join(binPath, "DmlProject.dll");
        var maps = importedMaps.ToImmutableDictionary(
            x => Path.GetRelativePath(absEnvPath, x),
            x => dmmSceneBuilder.Build(File.ReadAllText(x))
        );

        return new DmeProject(Assembly.LoadFile(asmBinPath), maps);
    }


    public static DmeProject CompileAndLoad(string path, IMudEntityBuilder builder,
        IImmutableDictionary<string, MacroDefinition>? buildMacros = null, bool disposeIntermediateCompile = true)
    {
        var binDir = Path.Join(path, ".\\bin");

        Directory.CreateDirectory(binDir);

        Compile(path, binDir, buildMacros, disposeIntermediateCompile);

        return Load(binDir, builder);
    }


    public static void Compile(string path, string outputDirectory,
        IImmutableDictionary<string, MacroDefinition>? buildMacros = null, bool disposeIntermediateCompile = true, bool generateSourceMap=true,
        bool globOnly = false)
    {
        var absEnvPath = Path.GetFullPath(path);
        var environmentFiles = Directory.GetFiles(absEnvPath, "*.dme");

        if (environmentFiles.Length != 1)
            throw new Exception("A project should only have one DME file. Loading failed.");

        var preprocessorCtx = new DmePreprocessContext(absEnvPath);

        var sourceFile = preprocessorCtx.PreprocessFile(environmentFiles[0], buildMacros).AsPlainText(generateSourceMap);

        Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(Path.Join(outputDirectory, "srcGlob.txt"), sourceFile);

        if (!globOnly)
        {
            var asmBinPath = Path.Join(outputDirectory, "DmlProject.dll");
            MsBuildDmlCompiler.Compile(sourceFile, asmBinPath, disposeIntermediateCompile);
        }

        foreach(var map in preprocessorCtx.ImportedMaps)
            File.Copy(map, Path.Join(outputDirectory, Path.GetFileName(map)), true);
    }

    public static void CompileGlob(string globFile, string binDir, bool disposeIntermediateCompile=true)
    {
        var asmBinPath = Path.Join(binDir, "DmlProject.dll");
        MsBuildDmlCompiler.Compile(File.ReadAllText(globFile), asmBinPath, disposeIntermediateCompile);
    }
}