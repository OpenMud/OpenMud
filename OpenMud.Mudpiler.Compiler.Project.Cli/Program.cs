// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using OpenMud.Mudpiler.Compiler.Project.Cli;

class Program
{
    static void Main(string[] args)
    {
        // Build a configuration object from command line
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        var npm = new NpmWrapper(config["npm"]);

        if (!npm.IsNpmInstalled())
        {
            Console.Error.WriteLine("Error, command 'npm' was not found on the system. Please ensure NPM is installed and try again.");
            Environment.Exit(1);
            return;
        }

        var projectDirectory = config["project"] ?? Directory.GetCurrentDirectory();
        var task = config["task"] ?? "host";

        Console.WriteLine($"Project directory is: {projectDirectory}");
    }
}
