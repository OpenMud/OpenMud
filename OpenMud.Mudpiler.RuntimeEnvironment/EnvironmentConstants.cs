using System.Collections.Immutable;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public static class EnvironmentConstants
{
    public static readonly int OBJ_LAYER = 10000;
    public static readonly int TRUE = 1;
    public static readonly int FALSE = 0;
    public static readonly int TOPDOWN_MAP = 1;

    public static readonly int NORTH = 1;
    public static readonly int SOUTH = 2;
    public static readonly int EAST = 4;
    public static readonly int WEST = 8;
    public static readonly int NORTHEAST = 5;
    public static readonly int NORTHWEST = 9;
    public static readonly int SOUTHEAST = 6;
    public static readonly int SOUTHWEST = 10;
    public static readonly int UP = 10;
    public static readonly int DOWN = 32;


    public static readonly IImmutableDictionary<string, MacroDefinition> BUILD_MACROS =
        new Dictionary<string, MacroDefinition>
        {
            { "OBJ_LAYER", new MacroDefinition(OBJ_LAYER.ToString()) },
            { "TRUE", new MacroDefinition(TRUE.ToString()) },
            { "FALSE", new MacroDefinition(FALSE.ToString()) },
            { "TOPDOWN_MAP", new MacroDefinition(TOPDOWN_MAP.ToString()) },

            { "NORTH", new MacroDefinition(NORTH.ToString()) },
            { "SOUTH", new MacroDefinition(SOUTH.ToString()) },
            { "EAST", new MacroDefinition(EAST.ToString()) },
            { "WEST", new MacroDefinition(WEST.ToString()) },
            { "NORTHEAST", new MacroDefinition(NORTHEAST.ToString()) },
            { "NORTHWEST", new MacroDefinition(NORTHWEST.ToString()) },
            { "SOUTHEAST", new MacroDefinition(SOUTHEAST.ToString()) },
            { "SOUTHWEST", new MacroDefinition(SOUTHWEST.ToString()) },
            { "UP", new MacroDefinition(UP.ToString()) },
            { "DOWN", new MacroDefinition(DOWN.ToString()) }
        }.ToImmutableDictionary();
}