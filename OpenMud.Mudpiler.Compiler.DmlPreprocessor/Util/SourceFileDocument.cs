using System.Text;
using System.Text.RegularExpressions;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public class SourceFileDocument
{
    public static readonly SourceFileDocument Empty = new(Enumerable.Empty<SourceFileDocument>());

    private readonly List<SourceFileLine> LineData;
    public readonly PreprocessorStateFlag StateFlag;
    public readonly int IntegerValue;
    private readonly StringBuilder Contents = new();
    private string? textualCache = null;
    private readonly DocumentCharacterMasking masking;

    public string Textual
    {
        get
        {
            if (textualCache == null)
                textualCache = Contents.ToString();

            return textualCache!;
        }
    }

    public SourceFileDocument(IEnumerable<SourceFileDocument> src, string delimeter = "\r\n")
    {
        StateFlag = PreprocessorStateFlag.Text;
        IntegerValue = 1;

        LineData = new List<SourceFileLine>();
        Contents = new StringBuilder();
        masking = new DocumentCharacterMasking();
        bool leadingLine = true;
        foreach (var doc in src)
        {
            int origin = Contents.Length;

            masking.Append(doc.masking);
            Contents.Append(doc.Contents);
            var sourceLineData = doc.LineData.Select(l => l.Offset(origin));

            if (!leadingLine)
                sourceLineData = sourceLineData.Skip(1);

            LineData.AddRange(sourceLineData);

            leadingLine = delimeter == "\r\n";

            Contents.Append(delimeter);
            masking.Append(delimeter.Length, false);
        }

    }

    private SourceFileDocument(IEnumerable<SourceFileLine> lineInfo, string contents, bool isMasked = false)
    {
        Contents = new StringBuilder(contents);
        StateFlag = PreprocessorStateFlag.Text;
        IntegerValue = 1;
        LineData = lineInfo.ToList();
        masking = new DocumentCharacterMasking(Contents.Length, isMasked);
    }

    private SourceFileDocument(PreprocessorStateFlag flag)
    {
        StateFlag = flag;
        LineData = new();
        IntegerValue = flag != PreprocessorStateFlag.False ? 1 : 0;
        Contents = new StringBuilder();
        masking = new DocumentCharacterMasking(Contents.Length, false);
    }

    private SourceFileDocument(int value)
    {
        StateFlag = PreprocessorStateFlag.Integer;
        LineData = new();
        IntegerValue = value;
        Contents = new StringBuilder();
        masking = new DocumentCharacterMasking(Contents.Length, false);
    }

    public static SourceFileDocument CreateStatus(bool status)
    {
        return new SourceFileDocument(status ? PreprocessorStateFlag.True : PreprocessorStateFlag.False);
    }

    public static SourceFileDocument CreateNumeric(int status)
    {
        return new SourceFileDocument(status);
    }

    public string Substring(int origin, int length)
    {
        var buffer = new char[length];

        Contents.CopyTo(origin, buffer, length);

        return new string(buffer);
    }

    public bool AsLogical() =>
        StateFlag != PreprocessorStateFlag.False &&
        (!TryNumeric(out var i) || i != 0) &&
        (!Contents.ToString().Any() || Contents.ToString().ToLower() != "false");

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
        }
        else if (!Contents.ToString().Any())
            return false;

        return int.TryParse(Contents.ToString(), out i);
    }

    public int Rewrite(int origin, int removeLength, string insert, bool isBlackedOut = false)
    {
        textualCache = null;

        insert = Regex.Replace(insert, "(?<!\r)\n", "\r\n");
        var sourceLineIndex = LineData.Select((x, i) => (x, i)).Last(x => x.x.Index <= origin).i;

        var sourceAddress = LineData[sourceLineIndex].Address;
        var startReplaceBoundaryIndex = LineData[sourceLineIndex].Index;
        var destLineIndex = LineData.Select((x, i) => (x, i)).Last(x => x.x.Index <= origin + removeLength).i;

        var replaceBoundaryIndex =
            destLineIndex + 1 < LineData.Count ? LineData[destLineIndex + 1].Index : Contents.Length;
        var replaceBoundaryLength = replaceBoundaryIndex - startReplaceBoundaryIndex;

        var removeCount = destLineIndex - sourceLineIndex + 1;

        for (var i = 0; i < removeCount; i++)
            LineData.RemoveAt(sourceLineIndex);

        var lineCompleteDataRaw = new char[replaceBoundaryLength];
        Contents.CopyTo(startReplaceBoundaryIndex, lineCompleteDataRaw, replaceBoundaryLength);
        Contents.Remove(startReplaceBoundaryIndex, replaceBoundaryLength);

        var lineCompleteData = new string(lineCompleteDataRaw);
        lineCompleteData = lineCompleteData.Substring(0, origin - startReplaceBoundaryIndex) + insert + lineCompleteData.Substring(origin - startReplaceBoundaryIndex + removeLength);

        Contents.Insert(startReplaceBoundaryIndex, lineCompleteData);

        var newContentLineLengths = Regex.Split(lineCompleteData, @"(?<=[\n])").Select(s => s.Length).ToList();

        if (newContentLineLengths.Last() == 0)
            newContentLineLengths.RemoveAt(newContentLineLengths.Count - 1);

        int rollingOffset = startReplaceBoundaryIndex;
        for (var i = 0; i < newContentLineLengths.Count; i++)
        {
            LineData.Insert(sourceLineIndex + i, new SourceFileLine(sourceAddress, rollingOffset));
            rollingOffset += newContentLineLengths[i];
        }

        int deltaOffset = insert.Length - removeLength;
        for (var i = sourceLineIndex + newContentLineLengths.Count; i < LineData.Count; i++)
            LineData[i] = LineData[i].Offset(deltaOffset);

        masking.Replace(origin, removeLength, insert.Length, isBlackedOut);

        return deltaOffset;
    }

    public static SourceFileDocument Create(string fileName, int origin, string contents, bool masked)
    {
        //Is origin used? How should it be used?
        contents = Regex.Replace(contents, "(?<!\r)\n", "\r\n");
        var srcLineLength = Regex.Split(contents, @"(?<=[\n])").Select(s => s.Length).ToArray();

        int textOffset = 0;
        int idx = 1;

        var lineInfo = new List<SourceFileLine>();
        foreach (var l in srcLineLength)
        {
            if (textOffset >= contents.Length)
                break;

            lineInfo.Add(new SourceFileLine(new SourceFileLineAddress(idx, fileName), textOffset));
            textOffset += l;
            idx++;
        }

        return new SourceFileDocument(lineInfo, contents, masked);
    }

    public string AsPlainText(bool injectLineDirective = true)
    {
        if (!injectLineDirective)
            return Contents.ToString();

        var working = new StringBuilder();
        var srcContents = Contents.ToString();
        //insert the directives in reverse. Saves us needing to update offsets sets.
        var workingLineData = LineData.OrderBy(x => x.Index).ToList();

        string formatDirective(SourceFileLineAddress a) => $"#line {a.Line} \"{a.FileName}\"\r\n";

        for (var i = 0; i < workingLineData.Count; i++)
        {
            int current = workingLineData[i].Index;
            int next = i + 1 >= workingLineData.Count ? -1 : workingLineData[i + 1].Index;

            var lineContent = next >= 0 ? srcContents.Substring(current, next - current) : srcContents.Substring(current);

            working.Append(formatDirective(workingLineData[i].Address));
            working.Append(lineContent);
        }

        return working.ToString();
    }

    public IEnumerable<char> EnumerateFrom(int v)
    {
        for (var i = v; i < Contents.Length; i++)
            yield return Contents[i];
    }

    public bool AllowReplace(int currentIdx, int length)
    {
        return masking.Accept(currentIdx, length);
    }
}