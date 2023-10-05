using System.Collections.Immutable;
using System.Reflection;
using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
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

    private string PreprocessContents(string resourceBasePath, IEnumerable<string> resourceDirectories, string contents,
        IImmutableDictionary<string, MacroDefinition>? predef,
        out IImmutableDictionary<string, MacroDefinition> resultantMacro)
    {
        return Preprocessor.Preprocess(resourceBasePath, resourceDirectories, contents, ResolveResourceDirectory,
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

        throw new Exception("Resouree could not be found: " + path);
    }

    private (IImmutableDictionary<string, MacroDefinition> macros, string importBody) ProcessImport(
        IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib,
        string fileName)
    {
        if (fileName.ToLower().EndsWith(".dmm"))
        {
            importedMaps.Add(Path.Combine(workingDirectory, fileName));
            return (dict, "");
        }

        var r = PreprocessFile(fileName, resourceDirectories, dict, out var resultant);
        return (resultant, r);
    }

    public string PreprocessFile(string dmeFile, IEnumerable<string> resourceDirectories,
        IImmutableDictionary<string, MacroDefinition>? predef,
        out IImmutableDictionary<string, MacroDefinition> resultantMacro)
    {
        var rsrcPath = Path.GetDirectoryName(dmeFile) ?? "./";

        return new DmePreprocessContext(Path.Join(workingDirectory, Path.GetDirectoryName(dmeFile) ?? "./"),
                importedMaps)
            .PreprocessContents(rsrcPath, resourceDirectories, File.ReadAllText(dmeFile), predef, out resultantMacro);
    }

    public string PreprocessFile(string dmeFile, IImmutableDictionary<string, MacroDefinition>? predef = null,
        IEnumerable<string> resourceDirectories = null)
    {
        var rsrcPath = Path.Combine(workingDirectory, Path.GetDirectoryName(dmeFile) ?? "./");

        return new DmePreprocessContext(rsrcPath, importedMaps)
            .PreprocessContents(rsrcPath, resourceDirectories ?? Enumerable.Empty<string>(), File.ReadAllText(dmeFile),
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

        return rootCtx.PreprocessFile(dmeFile);
    }

    public static DmeProject Load(string path, IMudEntityBuilder builder)
    {
        var absEnvPath = Path.GetFullPath(path);
        var environmentFiles = Directory.GetFiles(absEnvPath, "*.dme");

        if (environmentFiles.Length != 1)
            throw new Exception("A project should only have one DME file. Loading failed.");

        //Todo: This is kind of a hack. We shouldnt need to preprocess all the source fildes to find the maps...
        //but because they are not currently being compiled into an intermediate form, this is necessary.

        var preprocessorCtx = new DmePreprocessContext(absEnvPath);
        var sourceFile = preprocessorCtx.PreprocessFile(environmentFiles[0], EnvironmentConstants.BUILD_MACROS);

        var dmmSceneBuilder = new DmmSceneBuilderFactory(builder);

        var binDir = Path.Join(path, ".\\bin");
        var asmBinPath = Path.Join(binDir, "DmlProject.dll");
        var maps = preprocessorCtx.ImportedMaps.ToImmutableDictionary(
            x => Path.GetRelativePath(absEnvPath, x),
            x => dmmSceneBuilder.Build(File.ReadAllText(x))
        );

        return new DmeProject(Assembly.LoadFile(asmBinPath), maps);
    }


    public static DmeProject Compile(string path, IMudEntityBuilder builder,
        IImmutableDictionary<string, MacroDefinition>? buildMacros = null, bool disposeIntermediateCompile = true)
    {
        var absEnvPath = Path.GetFullPath(path);
        var environmentFiles = Directory.GetFiles(absEnvPath, "*.dme");

        if (environmentFiles.Length != 1)
            throw new Exception("A project should only have one DME file. Loading failed.");

        var preprocessorCtx = new DmePreprocessContext(absEnvPath);

        var sourceFile = preprocessorCtx.PreprocessFile(environmentFiles[0], buildMacros);

        var dmmSceneBuilder = new DmmSceneBuilderFactory(builder);
        var maps = preprocessorCtx.ImportedMaps.ToImmutableDictionary(
            x => Path.GetRelativePath(absEnvPath, x),
            x => dmmSceneBuilder.Build(File.ReadAllText(x))
        );


        var binDir = Path.Join(path, ".\\bin");

        Directory.CreateDirectory(binDir);

        File.WriteAllText(Path.Join(binDir, "srcGlob.txt"), sourceFile);

        var asmBinPath = Path.Join(binDir, "DmlProject.dll");
        MsBuildDmlCompiler.Compile(sourceFile, asmBinPath, disposeIntermediateCompile);

        return new DmeProject(Assembly.LoadFile(asmBinPath), maps);
    }
}