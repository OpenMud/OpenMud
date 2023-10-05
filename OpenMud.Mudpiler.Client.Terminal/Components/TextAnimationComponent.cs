using SadRogue.Primitives;

namespace OpenMud.Mudpiler.Client.Terminal.Components;

public struct TextAnimationFrame
{
    public readonly char Symbol;
    public readonly Color Colour;
    public readonly Color? BackgroundColour;

    public TextAnimationFrame(char symbol, Color colour, Color? backgroundColour)
    {
        Symbol = symbol;
        Colour = colour;
        BackgroundColour = backgroundColour;
    }
}

public class TextAnimationComponent
{
    public readonly TextAnimationFrame[] Frames;
    public readonly string ParsedText;
    public int FrameIndex;

    public float TimeSinceToggle;

    public TextAnimationComponent(TextAnimationFrame[] frames, string parsedText)
    {
        Frames = frames;
        ParsedText = parsedText;
    }
}