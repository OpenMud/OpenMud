using SadRogue.Primitives;

namespace OpenMud.Mudpiler.Client.Terminal.Components;

public struct CharacterGraphicComponent
{
    public readonly Color Colour;
    public readonly Color? BackgroundColour;
    public readonly char Text;

    public CharacterGraphicComponent(Color colour, Color? backgroundColour, char text)
    {
        Colour = colour;
        BackgroundColour = backgroundColour;
        Text = text;
    }
}