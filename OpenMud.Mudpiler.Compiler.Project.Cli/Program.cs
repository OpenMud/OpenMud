// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using OpenMud.Mudpiler.Compiler.Project.Cli;
using System.IO;
using SmartFormat;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Settings;
using SmartFormat.Extensions;


[Verb("build", HelpText = "Build the server logic module")]
class BuildOptions
{ //normal options here
}

[Verb("run", HelpText = "Run the game logic server")]
class RunOptions
{
    [Option("debug", Default = false)]
    public bool Debug { get; set; }
}

class CreateTemplateConfiguration
{
    public readonly string ProjectName;
    public readonly string LogicalProjectName;
    public readonly string JSLogicalProjectName;

    public CreateTemplateConfiguration(CreateOptions opts)
    {
        this.ProjectName = opts.ProjectName;
        this.LogicalProjectName = StringUtil.AsLogical(opts.ProjectName);
        this.JSLogicalProjectName = StringUtil.AsJsLogical(opts.ProjectName);
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
        ".dm"
    };

    public static readonly string SCAFFOLDING_REPO = "https://github.com/JeremyWildsmith/OpenMud.git";

    static int Main(string[] args) =>
        Parser.Default.ParseArguments<CreateOptions, RunOptions, BuildOptions>(args)
            .MapResult(
                (CreateOptions options) => DoCreate(options),
                (RunOptions options) => DoRun(options),
                (BuildOptions options) => DoBuild(options),
                errors => 1);

    private static int DoBuild(object opts)
    {
        throw new NotImplementedException();
    }

    private static int DoRun(object opts)
    {
        throw new NotImplementedException();
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

    private static SmartFormatter GetTemplater()
    {
        var sf = new SmartFormatter(new SmartSettings
        {
            StringFormatCompatibility = true,

            Parser = new SmartFormat.Core.Settings.ParserSettings()
            {
                ConvertCharacterStringLiterals = false,
                ErrorAction = ParseErrorAction.MaintainTokens
            }
        });

        sf.AddExtensions(new ReflectionSource());
        //        ISource w =  new ReflectionSource()
        sf.AddExtensions(new DefaultFormatter());
        //var w = sf.GetSourceExtensions();

        return sf;
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
        Console.WriteLine($"Scaffolding directory: {target.Name}");

        foreach (DirectoryInfo dir in source.GetDirectories())
            Scaffold(opts, dir, target.CreateSubdirectory(dir.Name));

        foreach (FileInfo file in source.GetFiles())
            CopyAndScaffoldTo(new CreateTemplateConfiguration(opts), file, Path.Combine(target.FullName, file.Name));
    }

    private static void CopyAndScaffoldTo(CreateTemplateConfiguration opt, FileInfo file, string destination)
    {
        var sf = GetTemplater();
        destination = sf.Format(destination, opt);

        var doScaffold = SCAFFOLD_EXTS.Any(file.Name.ToLower().EndsWith);

        if(doScaffold)
            File.WriteAllText(destination, sf.Format(File.ReadAllText(file.FullName), opt));
        else
            File.Copy(file.FullName, destination, true);
    }
}
