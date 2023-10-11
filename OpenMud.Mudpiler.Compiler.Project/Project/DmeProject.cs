using System.Collections.Immutable;
using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;
using OpenMud.Mudpiler.Compiler.Project.Scene;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Compiler.Project.Project;

public class DmePreprocessContext
{
    private readonly ISet<string> importedMaps;
    private readonly string workingDirectory;

    public DmePreprocessContext(string workingDirectory, ISet<string>? maps = null)
    {
        this.workingDirectory = workingDirectory;

        importedMaps = maps ?? new HashSet<string>();
    }

    public IEnumerable<string> ImportedMaps => importedMaps;

    private SourceFileDocument PreprocessContents(string fullFilePath, string resourceBasePath, IEnumerable<string> resourceDirectories, string contents,
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

        throw new Exception("Resource could not be found: " + path);
    }

    private (IImmutableDictionary<string, MacroDefinition> macros, SourceFileDocument importBody) ProcessImport(
        IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib,
        string fileName)
    {
        fileName = Path.Combine(workingDirectory, fileName);

        if (fileName.ToLower().EndsWith(".dmm"))
        {
            importedMaps.Add(fileName);
            return (dict, SourceFileDocument.Empty);
        }

        var r = PreprocessFile(fileName, resourceDirectories, dict, out var resultant);
        return (resultant, r);
    }

    public SourceFileDocument PreprocessFile(string dmeFile, IEnumerable<string> resourceDirectories,
        IImmutableDictionary<string, MacroDefinition>? predef,
        out IImmutableDictionary<string, MacroDefinition> resultantMacro)
    {
        var rsrcPath = Path.GetDirectoryName(dmeFile) ?? "./";

        dmeFile = Path.GetFullPath(dmeFile);

        return new DmePreprocessContext(Path.GetDirectoryName(dmeFile) ?? workingDirectory,
                importedMaps)
            .PreprocessContents(dmeFile, rsrcPath, resourceDirectories, File.ReadAllText(dmeFile), predef, out resultantMacro);
    }

    public SourceFileDocument PreprocessFile(string dmeFile, IImmutableDictionary<string, MacroDefinition>? predef = null,
        IEnumerable<string> resourceDirectories = null)
    {
        var rsrcPath = Path.Combine(workingDirectory, Path.GetDirectoryName(dmeFile) ?? "./");

        dmeFile = Path.GetFullPath(dmeFile);

        return new DmePreprocessContext(rsrcPath, importedMaps)
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
        IImmutableDictionary<string, MacroDefinition>? buildMacros = null, bool disposeIntermediateCompile = true)
    {
        var absEnvPath = Path.GetFullPath(path);
        var environmentFiles = Directory.GetFiles(absEnvPath, "*.dme");

        if (environmentFiles.Length != 1)
            throw new Exception("A project should only have one DME file. Loading failed.");

        var preprocessorCtx = new DmePreprocessContext(absEnvPath);

        var sourceFile = preprocessorCtx.PreprocessFile(environmentFiles[0], buildMacros).AsPlainText();

        Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(Path.Join(outputDirectory, "srcGlob.txt"), sourceFile);

        var asmBinPath = Path.Join(outputDirectory, "DmlProject.dll");
        MsBuildDmlCompiler.Compile(sourceFile, asmBinPath, disposeIntermediateCompile);

        foreach(var map in preprocessorCtx.ImportedMaps)
            File.Copy(map, Path.Join(outputDirectory, Path.GetFileName(map)), true);
    }
}