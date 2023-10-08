using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Project.Cli
{
    public class NpmWrapper
    {
        private readonly string npmPath;

        public NpmWrapper(string? npmPath = null)
        {
            // Find the path to npm executable (assumes it's in the system PATH)
            this.npmPath = npmPath ?? CommandFinder.GetCommandPath("npm") ?? "npm";
        }

        public bool IsNpmInstalled()
        {
            try
            {
                // Run 'npm -v' to check if npm is installed
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = npmPath;
                    process.StartInfo.Arguments = "-v";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string RunNpmCommand(string command, string workingDirectory)
        {
            try
            {
                using Process process = new Process();
                process.StartInfo.FileName = npmPath;
                process.StartInfo.Arguments = command;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                string output = "";
                string error = "";

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output += e.Data + Environment.NewLine;
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error += e.Data + Environment.NewLine;
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return $"Output:\n{output}\nError:\n{error}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
