using System.Text;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;
using System.Text.RegularExpressions;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

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

    public string[] ParseOutArguments(string source, string macroMatch, int origin, out int length)
    {
        var allowMatch = Blackout.CreateBlackouts(source);

        if (source.Substring(origin, macroMatch.Length) != macroMatch)
            throw new Exception("Invalid macro application...");

        var args = new List<string>();
        int braceIndex = 0;
        var currentArg = new StringBuilder();
        length = macroMatch.Length;
        bool terminated = false;
        int idx = origin + macroMatch.Length;
        foreach(char c in source.Substring(origin + macroMatch.Length))
        {
            if (terminated)
                break;

            length++;
            int currentIdx = idx;
            idx++;
            if (!allowMatch.Allow(currentIdx))
            {
                currentArg.Append(c);
                continue;
            }

            switch (c)
            {
                case '(':
                    braceIndex++;
                    break;
                case ')':
                    braceIndex--;

                    if (braceIndex == 0)
                    {
                        args.Add(currentArg.ToString());
                        currentArg = new StringBuilder();
                        terminated = true;
                    }

                    break;
                case ',':
                    if (braceIndex == 1)
                    {
                        args.Add(currentArg.ToString());
                        currentArg = new StringBuilder();
                    }

                    break;
                default:
                    currentArg.Append(c);
                    break;
            }
        }

        if (!terminated)
            throw new Exception("Macro not terminated.");

        return args.ToArray();
    }

    internal SourceFileDocument Apply(UnpackedSourceFileDocument source, Match match)
    {
        var srcBuffer = source.Contents.Substring(match.Index, match.Length);
        var newText = Text;

        int replaceLength = match.Length;

        if (ArgList != null)
        {
            var srcArgList = ParseOutArguments(source.Contents, match.Value, match.Index, out replaceLength);

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


        return source.ReplaceAndPack(match.Index, replaceLength, newText);
    }
}