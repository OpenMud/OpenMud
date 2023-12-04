using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

internal class DmlTextProcessingVisitor : DmeParserBaseVisitor<IImmutableSourceFileDocument>
{
    private readonly string fileName;
    private bool _compilied = true;
    private readonly CommonTokenStream _tokensStream;

    public DmlTextProcessingVisitor(string fileName, CommonTokenStream tokensStream)
    {
        this.fileName = fileName;
        this._tokensStream = tokensStream;
    }

    public override IImmutableSourceFileDocument VisitDmlDocument([NotNull] DmeParser.DmlDocumentContext context)
    {
        var sb = new List<IImmutableSourceFileDocument>();

        foreach (DmeParser.TextContext text in context.text())
            sb.Add(Visit(text));

        return new ConcatSourceFileDocument(sb, "");
    }

    public override IImmutableSourceFileDocument VisitText([NotNull] DmeParser.TextContext context)
    {
        IImmutableSourceFileDocument result = SourceFileDocument.Create(fileName, context.Start.Line, _tokensStream.GetText(context), false);

        var directive = false;

        if (!_compilied || directive)
        {
            result = SourceFileDocument.Create(fileName, context.Start.Line, "\r\n", false);
        }

        if (_compilied && !directive)
        {
            //In this case, it is not a macro and we are compiling it.
            var cb = context.code_block();

            if (cb == null)
                result = new EmptySourceFileDocument();
            else
                result = Visit(cb);
        }

        return result;
    }

    public override IImmutableSourceFileDocument VisitCode_block([NotNull] DmeParser.Code_blockContext context)
    {
        var r = context.code().Select(Visit).ToList();

        if (r.Count == 0)
            return new EmptySourceFileDocument();

        return new SourceFileDocument(context.code().Select(Visit), "");
    }

    public override IImmutableSourceFileDocument VisitCode([NotNull] DmeParser.CodeContext context)
    {
        if (context.@string() != null)
            return Visit(context.@string());

        if (context.resource() != null)
        {
            var src = context.resource();
            return SourceFileDocument.Create(fileName, src.Start.Line, src.GetText(), true);
        }

        var code = context.code_literal();
        if (code != null) {
            var pieces = code.Select(t =>
                SourceFileDocument.Create(fileName, t.Start.Line, t.GetText(), false)
            );

            var document = new ConcatSourceFileDocument(pieces, "");

            return document;
        }

        throw new Exception("Unknown code type");
    }

    public override IImmutableSourceFileDocument VisitString_expression([NotNull] DmeParser.String_expressionContext context)
    {
        var r = Visit(context.code_block());

        var start = SourceFileDocument.Create(fileName, context.Start.Line, $"text(", true);
        var end = SourceFileDocument.Create(fileName, context.Start.Line, ")", true);
        return new ConcatSourceFileDocument(new[] { start, r, end }, "");
    }

    private string EscapeString(string str)
    {
        var r = new StringBuilder();
        bool escaped = false;

        foreach(var c in str)
        {
            if (c == '\\' && !escaped)
            {
                escaped = true;
                r.Append(c);
                continue;
            }

            if (c == '"' && !escaped)
                r.Append('\\');

            r.Append(c);
            escaped = false;
        }

        return r.ToString();
    }

    private IImmutableSourceFileDocument CreateStringConcat(int line, List<IImmutableSourceFileDocument> contents)
    {
        if (contents.Count == 0)
            return SourceFileDocument.Create(fileName, line, $"\"\"", true);

        if (contents.Count > 1)
        {
            var components = new List<IImmutableSourceFileDocument>();
            var start = SourceFileDocument.Create(fileName, line, $"addtext(", true);

            components.Add(start);
            for (var i = 0; i < contents.Count; i++)
            {
                components.Add(contents[i]);

                if (i != contents.Count - 1)
                {
                    var delim = SourceFileDocument.Create(fileName, line, $",", true);
                    components.Add(delim);
                }
            }

            var end = SourceFileDocument.Create(fileName, line, $")", true);
            components.Add(end);

            return new ConcatSourceFileDocument(components, "");
        }

        return contents.Single();
    }

    public override IImmutableSourceFileDocument VisitString_contents_constant([NotNull] DmeParser.String_contents_constantContext context)
    {
        var contents = context.GetText();
        var contentLines = Regex.Split(contents, "\r\n|\r|\n").Select(EscapeString).ToArray();

        //add newline text inbetween
        for (var i = 0; i < contentLines.Length - 1; i++)
            contentLines[i] = contentLines[i] + "\\r\\n";

        var netString = string.Join("", contentLines);
        return SourceFileDocument.Create(fileName, context.Start.Line, "\"" + netString + "\"", true);
    }

    public override IImmutableSourceFileDocument VisitString([NotNull] DmeParser.StringContext context)
    {
        var contents = context.string_contents().Select(Visit).ToList();

        return CreateStringConcat(context.Start.Line, contents);
    }
}