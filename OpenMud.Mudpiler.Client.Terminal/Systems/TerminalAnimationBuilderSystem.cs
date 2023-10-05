using System.Drawing;
using System.Text.RegularExpressions;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Client.Terminal.Components;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using Color = SadRogue.Primitives.Color;

namespace OpenMud.Mudpiler.Client.Terminal.Systems;

[With(typeof(LogicIdentifierComponent), typeof(VisibleComponent))]
internal class TerminalAnimationBuilderSystem : AEntitySetSystem<float>
{
    private static readonly Regex fontStyleRegex = new(@"(<font [^>]+>)", RegexOptions.IgnoreCase);
    private static readonly Regex fontStyleTagRegex = new(@"([a-z]+)=([^>\s]+)");
    private static readonly Color DEFAULT_COLOR = Color.White;
    private readonly LogicDirectory logicLookup;

    public TerminalAnimationBuilderSystem(World world, LogicDirectory logicLookup, bool useBuffer = false) : base(world,
        useBuffer)
    {
        this.logicLookup = logicLookup;
    }

    private static void ParseFormatter(string fontTag, Color currentColour, Color? currentBackgroundColour,
        out Color nextColor, out Color? nextBgColor)
    {
        nextColor = currentColour;
        nextBgColor = currentBackgroundColour;

        foreach (Match m in fontStyleTagRegex.Matches(fontTag))
        {
            var key = m.Groups[1].Value.ToLower();
            var val = m.Groups[2].Value;

            if (key == "color")
            {
                var color = ColorTranslator.FromHtml(val);
                nextColor = new Color(color.R, color.G, color.B);
            }
            else if (key == "bgcolor")
            {
                var color = ColorTranslator.FromHtml(val);
                nextBgColor = new Color(color.R, color.G, color.B);
            }
        }
    }

    private static TextAnimationComponent ParseAnimation(string text)
    {
        var formatters = fontStyleRegex.Matches(text).ToDictionary(x => x.Index, x => x);

        var styleColour = DEFAULT_COLOR;
        Color? styleBackgroundColour = null;

        List<TextAnimationFrame> frames = new();

        for (var i = 0; i < text.Length; i++)
            if (formatters.ContainsKey(i))
            {
                ParseFormatter(formatters[i].Value, styleColour, styleBackgroundColour, out styleColour,
                    out styleBackgroundColour);
                i += formatters[i].Length - 1;
            }
            else
            {
                frames.Add(new TextAnimationFrame(text[i], styleColour, styleBackgroundColour));
            }

        return new TextAnimationComponent(frames.ToArray(), text);
    }

    protected override void Update(float state, in Entity entity)
    {
        var logic = logicLookup[entity.Get<LogicIdentifierComponent>().LogicInstanceId];

        var text = logic["text"] as string;

        if (text == null)
        {
            text = logic["type"] as string;
            if (text != null)
                text = DmlPath.ResolveBaseName(text)[0].ToString();
        }

        if (text == null)
            text = "?";

        if (!entity.Has<TextAnimationComponent>() || entity.Get<TextAnimationComponent>().ParsedText != text)
            entity.Set(ParseAnimation(text));
    }
}