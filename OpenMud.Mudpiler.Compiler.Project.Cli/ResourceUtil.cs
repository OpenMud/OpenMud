using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Project.Cli
{
    internal static class ResourceUtil
    {
        internal static void ExtractEmbeddedFile(string resName, string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);

            Assembly assembly = Assembly.GetExecutingAssembly();

            using var input = assembly.GetManifestResourceStream(resName);
            using var output = File.Open(fileName, FileMode.CreateNew);

            if (input == null)
                throw new FileNotFoundException(resName + ": Embedded resoure file not found");

            var buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
            output.Flush();
        }
    }
}
