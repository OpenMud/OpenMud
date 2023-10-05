using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;

namespace OpenMud.Mudpiler.Client.Terminal.UI;

public class CommandTemplate
{
    public readonly string Display;
    public readonly string Template;

    public CommandTemplate(string display, string template)
    {
        Display = display;
        Template = template;
    }

    public override bool Equals(object? obj)
    {
        return obj is CommandTemplate template &&
               Display == template.Display;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Display);
    }

    public override string ToString()
    {
        return Display;
    }
}

public delegate void CommandSelected(string template);

public class CommandsWindow : Window
{
    private ListBox commands;
    private Func<Entity> getSubject;

    public CommandsWindow(int width, int height, Func<Entity> getSubject) : base(width, height)
    {
        if (width < 5 || height < 5)
            throw new ArgumentException("Too small");

        this.getSubject = getSubject;

        Title = "Commands";
        CanDrag = false;

        commands = new ListBox(width - 2, height - 2);
        commands.Position = new Point(1, 1);
        Controls.Add(commands);

        commands.SelectedItemExecuted += OnItemSelected;
    }

    public event CommandSelected? OnCommandSelected;

    private void OnItemSelected(object? sender, ListBox.SelectedItemEventArgs e)
    {
        if (OnCommandSelected != null)
            OnCommandSelected(((CommandTemplate)e.Item).Template);
    }

    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        var subject = getSubject();

        if (!subject.IsAlive || !subject.Has<ActionableCommandsComponent>())
        {
            commands.Items.Clear();
            return;
        }

        var actionable = subject.Get<ActionableCommandsComponent>();

        var newOrder = actionable.Commands
            .OrderBy(x => x.Target == null ? 0 : 1)
            .ThenBy(x => x.Verb)
            .ThenBy(x => x.Target)
            .Select(x => new CommandTemplate(
                $"{x.Verb} {x.TargetName ?? "(self)"}",
                x.Target == null
                    ? $"{x.Verb}"
                    : $"{x.Verb} {x.TargetName}"
            ));

        var currentSelected = commands.SelectedItem;
        commands.Items.Clear();

        foreach (var i in newOrder)
            commands.Items.Add(i);

        if (currentSelected != null)
        {
            var idx = commands.Items.IndexOf(currentSelected);
            if (idx >= 0)
                commands.SelectedIndex = idx;
        }
    }
}