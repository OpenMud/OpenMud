using Microsoft.VisualBasic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public interface IImmutableSourceFileDocument
{
    public IEnumerable<SourceFileLine> LineData { get; }
    public IEnumerable<char> Textual { get; }
    public PreprocessorStateFlag StateFlag { get; }
    public int IntegerValue { get; }
    public int Length { get; }
    bool TryNumeric(out int i);
    bool AsLogical();
    string Substring(int origin, int length);
    public IImmutableDocumentCharacterMasking Masking { get; }

    public string AsPlainText(bool injectLineDirective = true)
    {
        if (!injectLineDirective)
            return new string(Textual.ToArray());

        var working = new StringBuilder();
        var srcContents = new string(Textual.ToArray());
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
}

public class ConcatSourceFileDocument : IImmutableSourceFileDocument
{
    public IEnumerable<char> Textual
    {
        get
        {
            foreach (var doc in documents)
            {
                if (doc.Length == 0)
                    continue;

                foreach (var c in doc.Textual)
                    yield return c;

                foreach(var c in Delimeter)
                    yield return c;
            }
        }
    }
    public IImmutableDocumentCharacterMasking Masking
    {
        get
        {

            return new ConcatDocumentCharacterMasking(documents.Select(d => d.Masking));
        }
    }

    public PreprocessorStateFlag StateFlag => PreprocessorStateFlag.Text;

    public int IntegerValue => 1;

    public int Length
    {
        get
        {
            int length = 0;
            foreach (var doc in documents)
            {
                length += doc.Length + Delimeter.Length;
            }

            return length;
        }

    }


    private readonly IImmutableSourceFileDocument[] documents;
    private readonly string Delimeter;


    public IEnumerable<SourceFileLine> LineData
    {
        get
        {
            bool leadingLine = true;
            int origin = 0;
            bool any = false;
            foreach (var doc in documents)
            {
                if (doc.Length == 0)
                    continue;

                //masking.Append(doc.Masking);
                //Contents.Append(doc.Textual);
                var sourceLineData = doc.LineData.Select(l => l.Offset(origin)).ToList().AsEnumerable();

                origin += doc.Length + Delimeter.Length;

                if (!leadingLine)
                    sourceLineData = sourceLineData.Skip(1);

                foreach (var l in sourceLineData)
                {
                    any = true;
                    yield return l;
                }

                //lineData.AddRange(sourceLineData);

                leadingLine = Delimeter == "\r\n";

                //Contents.Append(delimeter);
                //masking.Append(delimeter.Length, false);
            }

            if (!any)
                yield return new SourceFileLine(SourceFileLineAddress.DEFAULT, 0);
        }
    }

    public ConcatSourceFileDocument(IEnumerable<IImmutableSourceFileDocument> src, string delimeter = "\r\n")
    {
        var documents = new List<IImmutableSourceFileDocument>();

        void Visit(IImmutableSourceFileDocument document)
        {
            if (document.Length == 0)
                return;

            if (document is ConcatSourceFileDocument c && c.Delimeter == this.Delimeter)
            {
                foreach (var d in c.documents)
                    Visit(d);
            }
            else
                documents.Add(document);
        }

        foreach(var s in src)
            Visit(s);

        this.documents = documents.ToArray();
        this.Delimeter = delimeter;
    }

    public bool AsLogical()
    {
        return true;
    }

    public string Substring(int origin, int length)
    {
        return new string(Textual.Skip(origin).Take(length).ToArray());
    }

    public bool TryNumeric(out int i)
    {
        i = 1;
        return true;
    }
}

public class EmptySourceFileDocument : IImmutableSourceFileDocument
{
    public IEnumerable<SourceFileLine> LineData => new[] { new SourceFileLine(SourceFileLineAddress.DEFAULT, 0) };

    public IEnumerable<char> Textual => Enumerable.Empty<char>();

    public PreprocessorStateFlag StateFlag => PreprocessorStateFlag.Text;

    public int IntegerValue => 1;


    public int Length => 0;

    public IImmutableDocumentCharacterMasking Masking => new DocumentCharacterMasking();

    public bool AsLogical()
    {
        return true;
    }

    public string Substring(int origin, int length)
    {
        return string.Empty;
    }

    public bool TryNumeric(out int i)
    {
        i = 1;
        return true;
    }
}

public class SourceFileDocument : IImmutableSourceFileDocument
{
    public IEnumerable<SourceFileLine> LineData => lineData;

    private readonly List<SourceFileLine> lineData;
    public PreprocessorStateFlag StateFlag { get; private set; }
    public IImmutableDocumentCharacterMasking Masking => masking;
    public int IntegerValue { get; private set; }
    public int Length => Contents.Length;
    private readonly StringBuilder Contents = new();

    private readonly DocumentCharacterMasking masking;

    private static readonly Regex NEWLINE = new("(?<!\r)\n");
    private static readonly Regex NEWLINE_TAIL = new(@"(?<=[\n])");


    public IEnumerable<char> Textual
    {
        get
        {
            for (var i = 0; i < Contents.Length; i++)
                yield return Contents[i];
        }
    }
    
    public SourceFileDocument(IEnumerable<IImmutableSourceFileDocument> src, string delimeter = "\r\n")
    {
        StateFlag = PreprocessorStateFlag.Text;
        IntegerValue = 1;

        lineData = new List<SourceFileLine>();
        masking = new DocumentCharacterMasking();
        bool leadingLine = true;
        foreach (var doc in src)
        {
            if (doc.Length == 0)
                continue;

            int origin = Contents.Length;

            masking.Append(doc.Masking);

            Contents.Append(doc.Textual.ToArray());
            var sourceLineData = doc.LineData.Select(l => l.Offset(origin));

            if (!leadingLine)
                sourceLineData = sourceLineData.Skip(1);

            lineData.AddRange(sourceLineData);

            leadingLine = delimeter == "\r\n";

            Contents.Append(delimeter);
            masking.Append(delimeter.Length, false);
        }

        if(lineData.Count == 0)
            lineData.Add(new SourceFileLine(SourceFileLineAddress.DEFAULT, 0));
    }

    private SourceFileDocument(IEnumerable<SourceFileLine> lineInfo, string contents, bool isMasked = false)
    {
        Contents = new StringBuilder(contents);
        StateFlag = PreprocessorStateFlag.Text;
        IntegerValue = 1;
        lineData = lineInfo.ToList();
        masking = new DocumentCharacterMasking(Contents.Length, isMasked);
    }

    private SourceFileDocument(PreprocessorStateFlag flag)
    {
        StateFlag = flag;
        lineData = new();
        IntegerValue = flag != PreprocessorStateFlag.False ? 1 : 0;
        Contents = new StringBuilder();
        masking = new DocumentCharacterMasking(Contents.Length, false);
    }

    private SourceFileDocument(int value)
    {
        StateFlag = PreprocessorStateFlag.Integer;
        lineData = new();
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

    public string CreateString()
    {
        return Contents.ToString();
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
        if (insert.Length == 0)
            return 0;

        insert = Regex.Replace(insert, "(?<!\r)\n", "\r\n");
        var sourceLineIndex = lineData.Select((x, i) => (x, i)).Last(x => x.x.Index <= origin).i;

        var sourceAddress = lineData[sourceLineIndex].Address;
        var startReplaceBoundaryIndex = lineData[sourceLineIndex].Index;
        var destLineIndex = lineData.Select((x, i) => (x, i)).Last(x => x.x.Index <= origin + removeLength).i;

        var replaceBoundaryIndex =
            destLineIndex + 1 < lineData.Count ? lineData[destLineIndex + 1].Index : Contents.Length;
        var replaceBoundaryLength = replaceBoundaryIndex - startReplaceBoundaryIndex;

        var removeCount = destLineIndex - sourceLineIndex + 1;

        for (var i = 0; i < removeCount; i++)
            lineData.RemoveAt(sourceLineIndex);

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
            lineData.Insert(sourceLineIndex + i, new SourceFileLine(sourceAddress, rollingOffset));
            rollingOffset += newContentLineLengths[i];
        }

        int deltaOffset = insert.Length - removeLength;
        for (var i = sourceLineIndex + newContentLineLengths.Count; i < lineData.Count; i++)
            lineData[i] = lineData[i].Offset(deltaOffset);

        masking.Replace(origin, removeLength, insert.Length, isBlackedOut);

        return deltaOffset;
    }
    
    public void Append(string text, bool masked = false)
    {
        Rewrite(Contents.Length, 0, text, masked);
    }

    public static SourceFileDocument Create(string fileName, int origin, string contents, bool masked)
    {
        //Is origin used? How should it be used?
        contents = NEWLINE.Replace(contents, "\r\n");
        var srcLineLength = NEWLINE_TAIL.Split(contents).Select(s => s.Length).ToArray();

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

        if(lineInfo.Count == 0)
            lineInfo.Add(new SourceFileLine(new SourceFileLineAddress(1, fileName), 0));

        return new SourceFileDocument(lineInfo, contents, masked);
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

    internal bool IsBlackout(int currentIdx) => !AllowReplace(currentIdx, 1);
}