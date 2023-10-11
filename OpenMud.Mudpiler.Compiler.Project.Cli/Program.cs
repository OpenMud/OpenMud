// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using OpenMud.Mudpiler.Compiler.Project.Cli;
using System.IO;
using System.Reflection;
using System.Resources;
using OpenMud.Mudpiler.Compiler.Asset;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Net.Server;
using OpenMud.Mudpiler.RuntimeEnvironment;
using Scriban;


[Verb("build", HelpText = "Build the server logic module")]
class BuildOptions
{
    [Option("project", Required = false, HelpText = "Path to project directory")]
    public string? Project { get; set; }
    [Option("resources", Required = false, Default = false, HelpText = "Build resources only")]
    public bool resources { get; set; }

}

[Verb("run", HelpText = "Run the game logic server")]
class RunOptions
{
    [Option("debug", Default = false)]
    public bool Debug { get; set; }

    [Option("project", Required = false, HelpText = "Path to project directory")]
    public string? Project { get; set; }


    [Option( "urls", HelpText = "Urls to host game server on")]
    public IEnumerable<string> Urls { get; set; }
}

class CreateTemplateConfiguration
{
    public readonly string ProjectName;
    public readonly string LogicalProjectName;
    public readonly string JsLogicalProjectName;
    public readonly string VscodeProject = ".vscode";

    public CreateTemplateConfiguration(CreateOptions opts)
    {
        this.ProjectName = opts.ProjectName;
        this.LogicalProjectName = StringUtil.AsLogical(opts.ProjectName);
        this.JsLogicalProjectName = StringUtil.AsJsLogical(opts.ProjectName);
    }
}

[Verb("create", HelpText = "Run the game logic server")]
class CreateOptions
{
    [Option("template", Required = false, HelpText = "Name of the template to use when generating the project files.")]
    public string? Template { get; set; }

    [Option("destination", Required = false, HelpText = "Where the project is created. Defaults to the current working directory.")]
    public string? Destination { get; set; }

    [Option("project", Required = true, HelpText = "Source project name")]
    public string ProjectName { get; set; }

    [Option("merge", Required = false, Default = false, HelpText = "Source project name")]
    public bool Merge { get; set; }
}

class Program
{
    private static readonly string[] SCAFFOLD_EXTS = new[]
    {
        ".cs",
        ".csproj",
        ".dml",
        ".dme",
        ".dm",
        ".json"
    };

    public static readonly string SCAFFOLDING_REPO = "https://github.com/JeremyWildsmith/OpenMud.git";

    static int Main(string[] args) =>
        Parser.Default.ParseArguments<CreateOptions, RunOptions, BuildOptions>(args)
            .MapResult(
                (CreateOptions options) => DoCreate(options),
                (RunOptions options) => DoRun(options),
                (BuildOptions options) => DoBuild(options),
                errors => 1);

    private static bool BuildAssets(BuildOptions opts)
    {
        var projectDirectory = opts.Project ?? Directory.GetCurrentDirectory();

        var game = Path.Join(projectDirectory, "./game");
        var clientAssets = Path.Join(projectDirectory, "./client/static/assets");
        var clientResourceDest = Path.Join(projectDirectory, "./client/src/");
        var clientResourceName = Path.Join(clientResourceDest, "./resources.ts");

        if (!Directory.Exists(game) || !Directory.Exists(clientAssets) || !Directory.Exists(clientResourceDest))
        {
            Console.Error.WriteLine(
                "Unable to find game and/or ./client/static/assets, ./client/src directory. Project malformed.");

            return false;
        }

        var fluidSynth = CommandFinder.GetCommandPath("fluidsynth");
        CliWrapper? fluidSynthCmd = fluidSynth == null ? null : new CliWrapper(fluidSynth);

        if (fluidSynthCmd == null || !fluidSynthCmd.IsInstalled())
        {
            Console.Error.WriteLine(
                "fluidsynth was not installed on the system or could not be found. Ensure fluidsynth is installed and available on the PATH environment variable.");

            return false;
        }

        var tempFolder = Directory.CreateTempSubdirectory();

        try
        {
            var soundFont = Path.Join(tempFolder.FullName, "./FluidR3Mono_GM.sf3");
            var soundFontLicense = Path.Join(tempFolder.FullName, "./FluidR3Mono_License.md");

            ResourceUtil.ExtractEmbeddedFile("OpenMud.Mudpiler.Compiler.Project.Cli.Resources.FluidR3Mono_GM.sf3", soundFont);
            ResourceUtil.ExtractEmbeddedFile("OpenMud.Mudpiler.Compiler.Project.Cli.Resources.FluidR3Mono_License.md", soundFontLicense);

            AssetCompiler.Compile(new AudioConverter(fluidSynth, soundFont), game, clientAssets, clientResourceName);
        }
        finally
        {
            tempFolder.DeleteReadOnly();
        }

        return true;
    }

    private static int DoBuild(BuildOptions opts)
    {
        if (!BuildAssets(opts))
        {
            Console.Error.WriteLine("Error building assets.");
            return 1;
        }

        if (opts.resources)
            return 0;

        BuildLogicAndMaps(opts);

        return 0;
    }

    private static int BuildLogicAndMaps(BuildOptions opts)
    {
        var projectDirectory = opts.Project ?? Directory.GetCurrentDirectory();
        var game = Path.Join(projectDirectory, "./game");
        var binDir = Path.Join(projectDirectory, "./bin");

        if (!Directory.Exists(game))
        {
            Console.Error.WriteLine(
                "Unable to find game folder. Project malformed.");

            return 1;
        }

        Directory.CreateDirectory(binDir);

        DmeProject.Compile(game, binDir, EnvironmentConstants.BUILD_MACROS);

        return 0;
    }

    private static int DoRun(RunOptions opts)
    {
        var projectDirectory = opts.Project ?? Directory.GetCurrentDirectory();
        var binDir = Path.Join(projectDirectory, "./bin");

        if (!Directory.Exists(binDir))
        {
            Console.Error.WriteLine(
                "Unable to find bin folder. Make sure you ran a build operation on the project first.");

            return 1;
        }

        if (opts.Debug)
            System.Diagnostics.Debugger.Launch();

        var aspArgs = "";

        if (opts.Urls.Any())
            aspArgs = "--urls " + string.Join(" ", opts.Urls);

        var gameServer = ServerApplication.Create(binDir, aspArgs.Split(' '));

        gameServer.Run();

        return 0;
    }

    public static string? PromptTemplate(CreateOptions opts, IEnumerable<string> optionsEnum)
    {
        var options = optionsEnum.ToList();

        if (opts.Template != null)
        {
            if (options.Contains(opts.Template))
                return opts.Template;

            return null;
        }

        if (options.Count == 0)
            return null;

        while (true)
        {
            Console.WriteLine("The following templates are available. Please select one:");

            foreach (var o in options)
                Console.WriteLine($" * {o}");

            Console.Write("Template: ");

            var templateName = Console.ReadLine().Trim();

            var selected = options.FirstOrDefault(o =>
                string.Equals(o.ToLower(), templateName.ToLower(), StringComparison.InvariantCultureIgnoreCase));

            if(selected != null)
                return selected;

            Console.Error.WriteLine(
                "That is not one of the available templates. Please try again and make sure to type the template name properly.");

        }
    }

    private static int DoCreate(CreateOptions opts)
    {
        var gitPath = CommandFinder.GetCommandPath("git");
        CliWrapper? gitCmd = gitPath == null ? null : new CliWrapper(gitPath);

        if (gitCmd == null || !gitCmd.IsInstalled())
        {
            Console.Error.WriteLine(
                "GIT was not installed on the system or could not be found. GIT must be installed before you can create projects.");
            return 1;
        }

        var tempFolder = Directory.CreateTempSubdirectory();//.FullName;
        var destinationDirectory = opts.Destination ?? Directory.GetCurrentDirectory();

        try
        {
            //Grab the scaffolds from git
            gitCmd.RunCommand($"clone -n --depth=1 --filter=tree:0 \"{SCAFFOLDING_REPO}\" .", tempFolder.FullName);
            gitCmd.RunCommand("sparse-checkout set --no-cone scaffold", tempFolder.FullName);
            gitCmd.RunCommand("checkout", tempFolder.FullName);

            var scaffoldDir = Path.Join(tempFolder.FullName, "./scaffold");
            var template = PromptTemplate(opts, Directory.GetDirectories(scaffoldDir).Select(Path.GetFileName)!);

            var scaffoldSource = template == null ? null : Path.Join(scaffoldDir, template);

            if (scaffoldSource == null || !Path.Exists(scaffoldSource))
            {
                Console.Error.WriteLine($"Unknown template selected, or no templates available.");
                return 1;
            }

            if (!Path.Exists(destinationDirectory))
            {
                try
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Project destination directory did not exist and there was an error creating it: {ex}");
                    return 1;
                }
            }

            var reservedNames = Directory.EnumerateDirectories(scaffoldSource)
                .Concat(Directory.EnumerateFiles(scaffoldSource))
                .Select(Path.GetFileName)
                .Distinct()
                .ToHashSet();

            var destFiles = Directory.EnumerateDirectories(destinationDirectory)
                .Concat(Directory.EnumerateFiles(destinationDirectory))
                .Select(Path.GetFileName)
                .Distinct()
                .ToHashSet();

            var collision = destFiles.Intersect(reservedNames).ToHashSet();

            if (collision.Any() && !opts.Merge)
            {
                Console.Error.WriteLine(
                    "Error, destination directory is not empty and contents conflict with template. Either remove the contents, or use the --merge flag.");

                return 1;
            }

            Scaffold(opts, new DirectoryInfo(scaffoldSource),  new DirectoryInfo(destinationDirectory));

            Console.WriteLine("All done!");
            return 0;
        }
        finally
        {
            tempFolder.DeleteReadOnly();
        }
    }

    private static void Scaffold(CreateOptions opts, DirectoryInfo source, DirectoryInfo target)
    {
        var templateConfig = new CreateTemplateConfiguration(opts);
        Console.WriteLine($"Scaffolding directory: {target.Name}");

        foreach (DirectoryInfo dir in source.GetDirectories())
        {
            var dirName = Template.Parse(dir.Name).Render(templateConfig);
            Scaffold(opts, dir, target.CreateSubdirectory(dirName));
        }

        foreach (FileInfo file in source.GetFiles())
            CopyAndScaffoldTo(templateConfig, file, Path.Combine(target.FullName, file.Name));
    }

    private static void CopyAndScaffoldTo(CreateTemplateConfiguration opt, FileInfo file, string destination)
    {
        destination = Template.Parse(destination).Render(opt);

        var doScaffold = SCAFFOLD_EXTS.Any(file.Name.ToLower().EndsWith);

        if (doScaffold)
            File.WriteAllText(destination, Template.Parse(File.ReadAllText(file.FullName)).Render(opt));
        else
            File.Copy(file.FullName, destination, true);
    }
}
