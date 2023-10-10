using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Project.Cli
{
    public class CliWrapper
    {
        private readonly string commandName;

        public CliWrapper(string commandName = null)
        {
            // Find the path to npm executable (assumes it's in the system PATH)
            this.commandName = commandName;
        }

        public bool IsInstalled()
        {
            try
            {
                // Run 'npm -v' to check if npm is installed
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = commandName;
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

        public void RunCommand(string command, string workingDirectory)
        {
            using Process process = new Process();
            process.StartInfo.FileName = commandName;
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
                    Console.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.Error.WriteLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("Program exited with non-zero exit code.");
        
        }
    }
}
