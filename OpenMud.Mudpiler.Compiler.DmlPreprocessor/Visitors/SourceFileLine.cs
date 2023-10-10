using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

public struct SourceFileLineAddress
{
    public readonly int Line;
    public readonly string FileName;

    public SourceFileLineAddress(int line, string fileName)
    {
        this.Line = line;
        this.FileName = fileName;

        if(line <= 0)
            throw new ArgumentOutOfRangeException("line numbers start from 1.");
    }
}

public struct SourceFileLine
{
    public readonly SourceFileLineAddress Address;
    public readonly string Contents;

    public SourceFileLine(SourceFileLineAddress address, string contents)
    {
        this.Address = address;
        this.Contents = contents;
    }

    public SourceFileLine(string fileName, int line, string contents)
    {
        this.Address = new SourceFileLineAddress(line, fileName);

        while (contents.EndsWith("\n") || contents.EndsWith("\r"))
            contents = contents.Substring(0, contents.Length - 1);

        this.Contents = contents;
    }
}

public enum PreprocessorStateFlag
{
    Text,
    True,
    False,
    Integer
}

internal struct UnpackedSourceFileDocument
{
    public readonly string Contents;
    public readonly IImmutableList<SourceFileLineAddress> Addresses;

    public UnpackedSourceFileDocument(string contents, SourceFileLineAddress[] addresses)
    {
        this.Contents = contents;

        var lines = Regex.Split(contents, "\r\n|\n|\r");

        if (lines.Length != addresses.Length)
            throw new Exception("Unbalanced addresses & line numbers...");

        Addresses = addresses.ToImmutableList();
    }

    public static UnpackedSourceFileDocument Create(IEnumerable<SourceFileLine> lines)
    {
        var lineData = lines.ToArray();
        var contents = String.Join("\r\n", lineData.Select(l => l.Contents.ToString()));

        return new UnpackedSourceFileDocument(contents, lineData.Select(l => l.Address).ToArray());
    }

    private int ComputeLineNumber(int idx)
    {
        bool inEol = false;
        int lineNumber = 0;

        //foreach (var c in Contents)
        for(var i = 0; i <= idx && i < Contents.Length; i++)
        {
            char c = Contents[i];
            switch (c)
            {
                case '\r':
                    inEol = true;
                    break;

                case '\n':
                    if (inEol)
                    {
                        inEol = false;
                        lineNumber++;
                    }
                    else
                    {
                        lineNumber++;
                    }
                    break;
                default:
                    if (inEol)
                        lineNumber++;
                    inEol = false;
                    break;
            }
        }

        return lineNumber;
    }

    public SourceFileDocument ReplaceAndPack(Match m, string newContents)
    {
        return ReplaceAndPack(m.Index, m.Length, newContents);
    }



    public SourceFileDocument ReplaceAndPack(int startIndex, int length, string newContents)
    {
        var start = ComputeLineNumber(startIndex);
        var end = ComputeLineNumber(startIndex + length);

        var consumeLines = Math.Max(1, end - start);

        var lines = Regex.Split(Contents, "\r\n|\n|\r");

        var naturalOrder = lines.Take(start).ToArray();

        var replaceOffsetDest = naturalOrder.Select(l => l.Length + 2).Sum();
        
        var replaceTarget = string.Join("\r\n", lines.Skip(start).Take(consumeLines));
        int replaceOrigin = startIndex - replaceOffsetDest;
        
        replaceTarget = 
            replaceTarget.Substring(0, replaceOrigin) + 
            newContents + 
            replaceTarget.Substring(replaceOrigin + length);

        var injectLines = Regex.Split(replaceTarget, "\r\n|\n|\r");
        var injectAddress = Addresses[start];

        var trailingLines = lines.Skip(start + consumeLines).ToList();

        var newLineSet = new List<SourceFileLine>();

        for (var i = 0; i < naturalOrder.Length; i++)
            newLineSet.Add(new SourceFileLine(Addresses[i], naturalOrder[i]));


        for (var i = 0; i < injectLines.Length; i++)
            newLineSet.Add(new SourceFileLine(injectAddress, injectLines[i]));

        for (var i = 0; i < trailingLines.Count; i++)
            newLineSet.Add(new SourceFileLine(Addresses[end + i], trailingLines[i]));

        return new SourceFileDocument(newLineSet);

        //We do the replace, but everything after end, needs to be assigned a line number n - (start - n

        /*
        var start = ComputeLineNumber(m.Index);
        var end = ComputeLineNumber(m.Index + m.Length);

        var lines = Regex.Split(Contents, "\r\n|\n|\r");

        int lowestAffected = Int32.MaxValue;

        var newLineSet = new List<SourceFileLine>();

        var lowestInjectIndex = Int32.MaxValue;

        var sourceLine = new StringBuilder();

        var rollingIndex = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (i >= start && i <= end)
            {
                sourceLine.Append(lines[i]);
                continue;
            }

            newLineSet.Add(new SourceFileLine(Addresses[i], lines[i]));
            rollingIndex += lines[i].Length + 2; //Add 2 for \r\n
        }

        var newLineContents = Contents.Substring(0, m.Index) + newContents + Contents.;
        var injectLines = Regex.Split(newContents, "\r\n|\n|\r");

        var originAddress = Addresses[start];
        foreach(var l in injectLines.Reverse())
            newLineSet.Insert(start, new SourceFileLine(originAddress, l));

        return new SourceFileDocument(newLineSet);*/
    }
}

public struct SourceFileDocument
{
    public static readonly SourceFileDocument Empty = new SourceFileDocument(Enumerable.Empty<SourceFileLine>());

    public readonly IImmutableList<SourceFileLine> Contents;
    public readonly PreprocessorStateFlag StateFlag;
    public readonly int IntegerValue;

    public SourceFileDocument(IEnumerable<SourceFileLine> lines)
    {
        Contents = lines.ToImmutableList();
        StateFlag = PreprocessorStateFlag.Text;
        IntegerValue = 1;
    }

    private SourceFileDocument(PreprocessorStateFlag flag)
    {
        StateFlag = flag;
        Contents = ImmutableList<SourceFileLine>.Empty;
        IntegerValue = flag != PreprocessorStateFlag.False ? 1 : 0;
    }

    private SourceFileDocument(int value)
    {
        StateFlag = PreprocessorStateFlag.Integer;
        Contents = ImmutableList<SourceFileLine>.Empty;
        IntegerValue = value;
    }

    public static SourceFileDocument CreateStatus(bool status)
    {
        return new SourceFileDocument(status ? PreprocessorStateFlag.True : PreprocessorStateFlag.False);
    }

    public static SourceFileDocument CreateNumeric(int status)
    {
        return new SourceFileDocument(status);
    }

    public bool AsLogical() =>
        StateFlag != PreprocessorStateFlag.False &&
        (!TryNumeric(out var i) || i != 0) &&
        (!Contents.Any() || Contents.First().Contents.ToLower() != "false");

    public bool TryNumeric(out int i)
    {
        i = 0;

        if (StateFlag == PreprocessorStateFlag.True)
        {
            i = IntegerValue;
            return true;
        }
        else if (StateFlag == PreprocessorStateFlag.False)
        {
            i = 0;
            return true;
        }
        else if (StateFlag == PreprocessorStateFlag.True)
        {
            i = 1;
            return true;
        } else if (!Contents.Any())
            return false;

        return int.TryParse(Contents.First().Contents, out i);
    }

    internal UnpackedSourceFileDocument Unpack()
    {
        return UnpackedSourceFileDocument.Create(Contents);
    }

    public static SourceFileDocument Create(string fileName, int origin, string contents)
    {
        string[] lines = Regex.Split(contents, @"\r\n|\r");

        var sourceLines = lines.Select((l, i) => new SourceFileLine(fileName, i + origin, l)).ToImmutableList();

        return new SourceFileDocument(sourceLines);
    }

    public string AsPlainText(bool injectLineDirective = true)
    {
        return string.Join("\r\n", Contents.Select(c 
            =>
        {
            if (!injectLineDirective)
                return c.Contents;

            if (!c.Contents.Any())
                return null;

            var line = $"#line {c.Address.Line} \"{c.Address.FileName}\"\r\n{c.Contents}";

            return line;
        }).Where(x => x != null));
    }
}