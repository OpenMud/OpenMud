using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Core.Components;

public struct ParseCommandComponent
{
    public readonly string Source;
    public readonly string? Target = null;
    public readonly string Command;

    public ParseCommandComponent(string source, string? target, string command)
    {
        Source = source;
        Target = target;
        Command = command;
    }
}