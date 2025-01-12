using System.Reflection;
using DefaultEcs;
using OpenMud.Mudpiler.Client.Terminal.Components;
using OpenMud.Mudpiler.Client.Terminal.UI;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;
using SadConsole;
using SadConsole.Input;
using SadConsole.Quick;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using Keyboard = SadConsole.Input.Keyboard;

namespace OpenMud.Mudpiler.Client.Terminal;

public class Program
{
    private static ScreenObject container;

    private static GameSimulation gameSim;

    private static InteractionWindow logConsole;

    private static DmeProject project;

    private static bool HadInput;

    public static void Main(string[] args)
    {
        if (args.Contains("--skip-build"))
        {
            project = DmeProject.Load(Path.Join(Directory.GetCurrentDirectory(), "./bin"), new BaseEntityBuilder());   
        }
        else
        {
            project = DmeProject.CompileAndLoad(Directory.GetCurrentDirectory(), new BaseEntityBuilder(),
                EnvironmentConstants.BUILD_MACROS, false);    
        }
        
        Game.Create(160, 50);
        new Thread(GameRunner).Start(Game.Instance);
        
        while (true)
        {
            var gs = new DmeProgram(new TerminalGameFactory(project));
            gs.Start();
            Thread.Sleep(10000);
            gs.Shutdown();
            Thread.Sleep(3000);
        }

    }

    private static void GameRunner(object? o)
    {
        ((Game)o).Run();
    }
}