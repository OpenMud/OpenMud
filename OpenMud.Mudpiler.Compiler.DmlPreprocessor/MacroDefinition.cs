using System.Text.RegularExpressions;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor;

public class MacroDefinition
{
    public readonly string[]? ArgList;
    public readonly string Text;

    public MacroDefinition(string text, string[]? argList = null)
    {
        Text = text;
        ArgList = argList;
    }

    private string FindArgumentList(string source, Match match, out string[]? sourceArgList)
    {
        sourceArgList = null;

        if (ArgList == null)
            return source;

        var openBrace = source.IndexOf('(', match.Index);

        if (openBrace < 0 || source.Substring(match.Index, openBrace - match.Index).Trim().Length > 0)
            return source;

        var closeBrace = source.IndexOf(')', match.Index);

        if (closeBrace < 0)
            return source;

        sourceArgList = source.Substring(openBrace + 1, closeBrace - openBrace - 1).Split(",");

        return source.Remove(openBrace, closeBrace - openBrace + 1);
    }

    internal string Apply(string source, Match match)
    {
        var srcBuffer = source.Remove(match.Index, match.Length);
        var newText = Text;

        if (ArgList != null)
        {
            srcBuffer = FindArgumentList(srcBuffer, match, out var srcArgList);

            var argMapping = ArgList
                .Select((x, i) => (x, i))
                .ToDictionary(
                    x => new Regex(@"\b" + Regex.Escape(x.x) + @"\b"),
                    x => srcArgList == null || srcArgList.Length <= x.i ? "" : srcArgList[x.i]);

            while (true)
            {
                var nextApplication = argMapping
                    .SelectMany(x =>
                        x.Key
                            .Matches(newText)
                            .Select(m => Tuple.Create(x.Value, m))
                    )
                    .FirstOrDefault();

                if (nextApplication == null)
                    break;

                newText = newText.Remove(nextApplication.Item2.Index, nextApplication.Item2.Length);
                newText = newText.Insert(nextApplication.Item2.Index, nextApplication.Item1);
            }
        }

        return srcBuffer.Insert(match.Index, newText);
    }
}