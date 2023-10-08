using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Asset
{
    public class AudioConverter
    {
        private readonly string fluidSynthPath;
        private readonly string soundFont;

        public AudioConverter(string fluidSynthPath, string soundFont)
        {
            this.fluidSynthPath = fluidSynthPath;
            this.soundFont = soundFont;
        }

        public void ConvertMidiToWav(string midiFilePath, string wavOutputPath)
        {
            try
            {
                /*
                var fluidsynthpath =
                    @"C:\Users\jerem\OneDrive\Desktop\byond_test_projects\compile_tools\fluidsynth\bin\fluidsynth.exe";
                var soundfontpath =
                    @"C:\Users\jerem\OneDrive\Desktop\byond_test_projects\compile_tools\soundfont\FluidR3Mono_GM.sf3";
                */
                // Construct the command line arguments
                var arguments = $"\"{soundFont}\" -F \"{wavOutputPath}\" \"{midiFilePath}\"";

                // Create a ProcessStartInfo for the command
                var psi = new ProcessStartInfo
                {
                    FileName = fluidSynthPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Create and start the process
                using var process = new Process { StartInfo = psi };

                process.Start();
                process.WaitForExit();

                // Check if the conversion was successful (exit code 0)
                if (process.ExitCode == 0)
                    Console.WriteLine("Conversion completed successfully.");
                else
                    throw new Exception("Conversion failed. Check the tool and input MIDI file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
