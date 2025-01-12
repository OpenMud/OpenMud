using DefaultEcs;
using OpenMud.Mudpiler.Core;
using OpenMud.Mudpiler.Core.Components;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;

namespace OpenMud.Mudpiler.Client.Terminal.UI;

public class CommandTemplate
{
    public readonly string Display;
    public readonly string Template;
    public readonly string? Noun;
    public readonly string? Identifier;
    public readonly int Precedent;

    public CommandTemplate(string display, string template, string? noun, string? identifier, int precedent)
    {
        Display = display;
        Template = template;
        Noun = noun;
        Identifier = identifier;
        Precedent = precedent;
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

public delegate void CommandSelected(CommandTemplate template);

public class CommandsWindow : Window, ICommandNounSolver
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

        commands.SelectedItemChanged += OnItemSelected;
        commands.SelectedItemExecuted += OnItemExecuted;
    }

    public event CommandSelected? OnCommandSelected;

    public event CommandSelected? OnCommandExecuted;

    private void OnItemSelected(object? sender, ListBox.SelectedItemEventArgs e)
    {
        if (OnCommandSelected != null && e.Item != null)
            OnCommandSelected(((CommandTemplate)e.Item));
    }
    
    private void OnItemExecuted(object? sender, ListBox.SelectedItemEventArgs e)
    {
        if (OnCommandExecuted != null)
            OnCommandExecuted(((CommandTemplate)e.Item));
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
                    : $"{x.Verb} {x.TargetName}",
                x.TargetName,
                x.Target,
                x.Precedent
            ));

        var currentSelected = commands.SelectedItem;
        commands.Items.Clear();

        foreach (var i in newOrder)
            commands.Items.Add(i);

        if (currentSelected != null)
        {
            var idx = commands.Items.IndexOf(currentSelected);
            if (idx >= 0 && commands.SelectedIndex != idx)
                commands.SelectedIndex = idx;
        }
    }

    public string? ResolveNounToTarget(string? noun)
    {
        if (noun == null)
            return null;
        
        return commands.Items.Select(e => ((CommandTemplate)e))
            .OrderBy(c => c.Precedent)
            .Where(x => noun.ToLower().Equals(x.Noun))
            .Select(x => x.Identifier)
            .FirstOrDefault();
    }
}