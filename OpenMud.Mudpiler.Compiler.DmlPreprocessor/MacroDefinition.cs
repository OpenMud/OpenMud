using System.Text;
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

    public string[] ParseOutArguments(SourceFileDocument source, string macroMatch, int origin, out int length)
    {
        if (source.Substring(origin, macroMatch.Length) != macroMatch)
            throw new Exception("Invalid macro application...");

        var args = new List<string>();
        int braceIndex = 0;
        var currentArg = new StringBuilder();
        length = macroMatch.Length;
        bool terminated = false;
        int idx = origin + macroMatch.Length;

        foreach(char c in source.EnumerateFrom(origin + macroMatch.Length))
        {
            if (terminated)
                break;

            length++;
            int currentIdx = idx;
            idx++;


            if (source.IsBlackout(currentIdx))
            {
                currentArg.Append(c);
                continue;
            }

            switch (c)
            {
                case '(':
                    if (braceIndex != 0)
                        currentArg.Append(c);

                    braceIndex++;
                    break;
                case ')':
                    braceIndex--;

                    if (braceIndex != 0)
                        currentArg.Append(c);

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
                    else
                        currentArg.Append(c);

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

    internal void Apply(SourceFileDocument source, Match match)
    {
        var srcBuffer = source.Substring(match.Index, match.Length);
        var newText = Text;

        int replaceLength = match.Length;

        if (ArgList != null)
        {
            var srcArgList = ParseOutArguments(source, match.Value, match.Index, out replaceLength);

            var argMapping = ArgList
                .Select((x, i) => (x, i))
                .ToDictionary(
                    x => new Regex(@"\b" + Regex.Escape(x.x) + @"\b"),
                    x => srcArgList == null || srcArgList.Length <= x.i ? "" : srcArgList[x.i]);

            var applications = argMapping
                    .SelectMany(x =>
                        x.Key
                            .Matches(newText)
                            .Select(m => Tuple.Create(x.Value, m))
                    )
                    .OrderByDescending(x => x.Item2.Index)
                    .ToList();

            foreach (var nextApplication in applications)
            {
                newText = newText.Remove(nextApplication.Item2.Index, nextApplication.Item2.Length);
                newText = newText.Insert(nextApplication.Item2.Index, nextApplication.Item1);
            }
        }

        //We can have accidentally insert -- or ++ (since these are different operators.)

        var start = match.Index - 1;
        char? leading = start < 0 ? null : source.EnumerateFrom(start).FirstOrDefault();
        char? trailing = source.EnumerateFrom(start + replaceLength + 1).FirstOrDefault();
        char? insertLeading = newText.Length == 0 ? null : newText[0];
        char? insertTrailing = newText.Length == 0 ? null : newText[newText.Length - 1];

        var adjacentBlock = new HashSet<char>(new char[] { '-', '+' });

        if (leading != null && insertLeading == leading && adjacentBlock.Contains(leading.Value))
            newText = " " + newText;

        if (trailing != null && insertTrailing == trailing && adjacentBlock.Contains(trailing.Value))
            newText = newText + " ";

        source.Rewrite(match.Index, replaceLength, newText);
    }
}