using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Project.Cli
{
    class CommandFinder
    {
        public static string? GetCommandPath(string command)
        {
            string commandPath = null;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // On Windows, use PATHEXT environment variable to find executable extensions
                var paths = new[] { Environment.CurrentDirectory }
                    .Concat(Environment.GetEnvironmentVariable("PATH").Split(';'));

                var extensions = new[] { ".cmd", ".bat", ".exe", ".sh", "" };

                var combinations = paths.SelectMany(x => extensions,
                    (path, extension) => Path.Combine(path, command + extension));

                return combinations.FirstOrDefault(File.Exists);
            }
            else
            {
                // On Linux/macOS, search directories in the PATH environment variable
                var paths = Environment.GetEnvironmentVariable("PATH").Split(':');

                foreach (string path in paths)
                {
                    string fullPath = Path.Combine(path, command);

                    if (File.Exists(fullPath))
                    {
                        commandPath = fullPath;
                        break;
                    }
                }
            }

            return commandPath;
        }
    }
}
