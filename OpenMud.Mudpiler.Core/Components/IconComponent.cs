namespace OpenMud.Mudpiler.Core.Components;

public struct IconComponent
{
    public readonly string Icon;
    public readonly string State;

    public IconComponent(string newIcon, string newIconState) : this()
    {
        Icon = newIcon;
        State = newIconState;
    }
}