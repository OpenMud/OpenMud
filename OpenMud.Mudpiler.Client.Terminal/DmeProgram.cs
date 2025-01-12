using System.Collections.Concurrent;
using System.Reflection;
using DefaultEcs;
using OpenMud.Mudpiler.Client.Terminal.Components;
using OpenMud.Mudpiler.Client.Terminal.UI;
using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
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

public class DmeProgram
{
    private TerminalGameFactory gameSimFactory;
    
    private ScreenObject container;

    private IGameSimulation gameSim;

    private InteractionWindow logConsole;

    private bool hadInput;

    private volatile bool isShutdown = false;

    private bool isInited = false;

    public DmeProgram(TerminalGameFactory gameSimFactory)
    {
        this.gameSimFactory = gameSimFactory;
        
        isInited = false;
    }

    public void Start()
    {
        if (isInited)
            return;
        
        isInited = true;
        Game.Instance.FrameUpdate += InitHook;

    }

    private void InitHook(object? sender, GameHost e)
    {
        Init();
        
        Game.Instance.FrameUpdate += OnRootConsoleUpdate;
        Game.Instance.FrameRender += OnRootConsoleRender;

        Game.Instance.FrameUpdate -= InitHook;
    }

    private void ShutdownHook(object? sender, GameHost e)
    {
        Game.Instance.FrameUpdate -= ShutdownHook;
        Game.Instance.Screen.Children.Clear();

        isShutdown = true;
    }

    //This will be called from an outside thread.
    public void Shutdown()
    {
        Game.Instance.FrameUpdate -= OnRootConsoleUpdate;
        Game.Instance.FrameRender -= OnRootConsoleRender;
        Game.Instance.FrameUpdate -= InitHook;
        Game.Instance.FrameUpdate += ShutdownHook;

        while(!isShutdown)
            Thread.Sleep(100);
    }

    private void OnRootConsoleRender(object? sender, GameHost e)
    {
        gameSim.Render((float)e.UpdateFrameDelta.TotalSeconds);
        container.Render(e.UpdateFrameDelta);
    }

    private void Init()
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
        commands.Position = new Point(90, 0);
        commands.IsVisible = true;


        // First console
        var _mapWindow = new Window(60, 12);
        var _mapConsole = new DrawingArea(50, 10);
        _mapConsole.Position = new Point(1, 1);
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

        logConsole = new InteractionWindow(160, 30);
        logConsole.Position = new Point(0, 20);

        //commands.OnCommandSelected += x => logConsole.SetCommandText(x.Template);
        commands.OnCommandExecuted += x =>
        {
            logConsole.SetCommandText(x.Template);
            logConsole.Submit();
        };

        logConsole.OnSubmitCommand += c => OnSubmitCommand(commands, c);

        container.Children.Add(logConsole);
        container.Children.Add(commands);
        container.Children.Add(inventory);

        container.Children.MoveToTop(_mapWindow);

        gameSim = gameSimFactory.SetRenderTarget(_mapConsole).Create();

        var player = gameSim.CreateMob("player");
        player.Set<CameraComponent>();
        player.Set<PlayerCanImpersonateComponent>();
        player.Set<PlayerImpersonatingComponent>();

        gameSim.World.Subscribe<WorldEchoMessage>(OnWorldEcho);
        gameSim.World.Subscribe<EntityEchoMessage>(OnEntityEcho);
        
        gameSim.World.Subscribe<VerbRejectionMessage>(OnVerbRejection);
        gameSim.World.Subscribe<CommandRejectionMessage>(OnCommandRejection);
    }

    private void OnCommandRejection(in CommandRejectionMessage msg)
    {
        logConsole.RecordLog("Command Rejected: " + msg.Reason);
    }

    private void OnVerbRejection(in VerbRejectionMessage msg)
    {
        logConsole.RecordLog("Verb Rejected: " + msg.Reason);
    }

    private void OnEntityEcho(in EntityEchoMessage msg)
    {
        logConsole.RecordLog(msg.Message);
    }

    private void OnSubmitCommand(ICommandNounSolver nounSolver, string command)
    {
        gameSim.DispatchCommand("player", nounSolver, command);
    }

    private Entity GetPlayer()
    {
        return gameSim.GetEntity("player");
    }

    private void OnWorldEcho(in WorldEchoMessage msg)
    {
        logConsole.RecordLog(msg.Message);
    }

    private bool ProcessInput(IScreenObject @object, Keyboard keyboard)
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
            if (!hadInput)
                return false;

            hadInput = false;
        }
        else
        {
            hadInput = true;
        }

        gameSim.Slide("player", deltaX, deltaY);
        return true;
    }

    // Event handler for RLNET's Update event
    private void OnRootConsoleUpdate(object sender, GameHost e)
    {
        gameSim.Update((float)e.UpdateFrameDelta.TotalSeconds);
    }
}