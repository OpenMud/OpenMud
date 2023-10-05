using System.Reflection;
using DefaultEcs;
using OpenMud.Mudpiler.Client.Terminal.Components;
using OpenMud.Mudpiler.Client.Terminal.UI;
using OpenMud.Mudpiler.Compiler.Project.Project;
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

    public static void Main()
    {
        project = DmeProject.Compile(Directory.GetCurrentDirectory(), new BaseEntityBuilder(),
            EnvironmentConstants.BUILD_MACROS, false);
        // Setup the engine and create the main window.
        Game.Create(160, 50);

        // Hook the start event so we can add consoles to the system.
        Game.Instance.OnStart = Init;
        Game.Instance.FrameUpdate += OnRootConsoleUpdate;
        Game.Instance.FrameRender += OnRootConsoleRender;

        // Start the game.
        Game.Instance.Run();
        Game.Instance.Dispose();
    }

    private static void OnRootConsoleRender(object? sender, GameHost e)
    {
        gameSim.Render((float)e.UpdateFrameDelta.TotalSeconds);
        container.Render(e.UpdateFrameDelta);
    }

    public static void Init()
    {
        container = new ScreenObject();
        Game.Instance.Screen = container;
        Game.Instance.DestroyDefaultStartingConsole();

        var inventory = new Window(30, 15);
        inventory.Position = new Point(130, 0);
        inventory.Title = "Inventory";
        inventory.CanDrag = false;
        inventory.IsVisible = true;

        var commands = new CommandsWindow(30, 15, GetPlayer);
        commands.Position = new Point(130, 15);
        commands.IsVisible = true;


        // First console
        var _mapWindow = new Window(130, 40);
        var _mapConsole = new DrawingArea(120, 30);
        _mapConsole.Position = new Point(5, 5);
        _mapWindow.Position = new Point(0, 0);

        _mapWindow.UseMouse = true;
        _mapWindow.UseKeyboard = true;
        _mapWindow.WithKeyboard(ProcessInput);

        _mapWindow.MoveToFrontOnMouseClick = true;
        _mapWindow.IsVisible = true;
        _mapWindow.CanDrag = false;
        _mapWindow.Title = "Game World";

        _mapWindow.Controls.Add(_mapConsole);
        container.Children.Add(_mapWindow);

        logConsole = new InteractionWindow(160, 10);
        logConsole.Position = new Point(0, 40);

        commands.OnCommandSelected += logConsole.SetCommandText;

        logConsole.OnSubmitCommand += OnSubmitCommand;

        container.Children.Add(logConsole);
        container.Children.Add(commands);
        container.Children.Add(inventory);

        container.Children.MoveToTop(_mapWindow);

        gameSim = new GameSimulation(CreateSceneBuilder(), CreateDmlAssembly(), _mapConsole);

        var player = gameSim.CreatePlayer("player");
        player.Set<CameraComponent>();
        player.Set<PlayerCanImpersonateComponent>();
        player.Set<PlayerImpersonatingComponent>();

        gameSim.OnWorldEcho += OnWorldEcho;
        gameSim.OnEntityEcho += (eid, name, m) => OnWorldEcho(m);
    }

    private static void OnSubmitCommand(string command)
    {
        gameSim.DispatchCommand("player", command);
    }

    private static Entity GetPlayer()
    {
        return gameSim.GetEntity("player");
    }

    private static void OnWorldEcho(string message)
    {
        logConsole.RecordLog(message);
    }

    private static bool ProcessInput(IScreenObject @object, Keyboard keyboard)
    {
        var deltaX = 0;
        var deltaY = 0;

        if (keyboard.IsKeyDown(Keys.Up))
            deltaY = -1;

        if (keyboard.IsKeyDown(Keys.Down))
            deltaY = 1;

        if (keyboard.IsKeyDown(Keys.Right))
            deltaX = 1;

        if (keyboard.IsKeyDown(Keys.Left))
            deltaX = -1;

        if (deltaX == 0 && deltaY == 0)
        {
            if (!HadInput)
                return false;

            HadInput = false;
        }
        else
        {
            HadInput = true;
        }

        gameSim.Slide("player", deltaX, deltaY);
        return true;
    }

    private static IMudSceneBuilder CreateSceneBuilder()
    {
        var entityBuilder = new BaseEntityBuilder();
        var sceneBuilder = project.Maps.Values.Single();

        return sceneBuilder;
    }

    private static Assembly CreateDmlAssembly()
    {
        return project.Logic;
    }

    // Event handler for RLNET's Update event
    private static void OnRootConsoleUpdate(object sender, GameHost e)
    {
        gameSim.Update((float)e.UpdateFrameDelta.TotalSeconds);
    }
}