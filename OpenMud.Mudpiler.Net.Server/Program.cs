
using System.Collections.Immutable;
using Antlr4.Runtime.Misc;
using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.RuntimeTypes;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.Net.Core;
using OpenMud.Mudpiler.Net.Core.Components;
using OpenMud.Mudpiler.Net.Core.Encoding;
using OpenMud.Mudpiler.Net.Core.Encoding.Components;
using OpenMud.Mudpiler.Net.Core.Hubs;

namespace OpenMud.Mudpiler.Net.Server;

internal class Program
{
    private static void Main(string[] args)
    {
        var app = ServerApplication.Create(Directory.GetCurrentDirectory(), args);
        app.Run();
    }
}