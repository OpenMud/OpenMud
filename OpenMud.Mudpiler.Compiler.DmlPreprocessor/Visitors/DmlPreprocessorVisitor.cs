using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using static System.Net.Mime.MediaTypeNames;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

public delegate (IImmutableDictionary<string, MacroDefinition> macros, IImmutableSourceFileDocument importBody) ProcessImport(
    IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib, string fileName);

public delegate string ResolveResourceDirectory(List<string> knownFileDirs, string path);

internal class DmlPreprocessorVisitor : DmeParserBaseVisitor<IImmutableSourceFileDocument>
{
    private static readonly Regex resourceRegex = new(@"'[^'\r\n]*'");
    private readonly string fileName;
    private readonly ProcessImport processImport;
    private readonly ResolveResourceDirectory resolveResourceDirectory;
    private readonly List<string> resourceDirectories = new();
    private readonly string resourcePathBase;
    private bool _compilied = true;
    private readonly LinkedList<bool> _conditions = new();
    private readonly CommonTokenStream _tokensStream;

    private Dictionary<string, MacroDefinition> ConditionalSymbols = new();
    private Dictionary<string, Regex> MacroRegexCache = new();

    public DmlPreprocessorVisitor(string fileName, string resourcePathBase, IEnumerable<string> resourceDirectory,
        CommonTokenStream commonTokenStream, ResolveResourceDirectory resolveResourceDirectory,
        ProcessImport processImport, IImmutableDictionary<string, MacroDefinition> macros) :
        this(fileName, resourcePathBase, resourceDirectory, commonTokenStream, resolveResourceDirectory, processImport)
    {
        ConditionalSymbols = macros.ToDictionary(x => x.Key, x => x.Value);
    }

    public DmlPreprocessorVisitor(string fileName, string resourcePathBase, IEnumerable<string> resourceDirectory,
        CommonTokenStream commonTokenStream, ResolveResourceDirectory resolveResourceDirectory,
        ProcessImport processImport)
    {
        this.fileName = fileName;
        resourceDirectories = resourceDirectory.ToList();
        this.resourcePathBase = resourcePathBase;
        _conditions.AddFirst(true);
        _tokensStream = commonTokenStream;
        this.processImport = processImport;
        this.resolveResourceDirectory = resolveResourceDirectory;
    }

    public IImmutableDictionary<string, MacroDefinition> MacroDefinitions => ConditionalSymbols.ToImmutableDictionary();
    public IEnumerable<string> ResourceSearchDirectory => resourceDirectories;

    private static string NormalizeResourcePath(string path)
    {
        path = path.Replace("/", "\\");

        while (path.Contains("\\\\"))
            path = path.Replace("\\\\", "\\");

        while (path.StartsWith(".\\"))
            path = path.Substring(2);

        return path;
    }

    private static string ParseEscapedResource(string s)
    {
        return s.Substring(1, s.Length - 2)
            .Replace("\\\'", "'");
    }

    private static string EscapeResourceString(string s)
    {
        return s.Replace("'", "\\\'");
    }

    private string ApplyResourceMacros(string source)
    {
        var path = ParseEscapedResource(source);

        var resourcePath = resolveResourceDirectory(ResourceSearchDirectory.ToList(), path);
        resourcePath = NormalizeResourcePath(Path.GetRelativePath(resourcePathBase, resourcePath));

        return "'" + EscapeResourceString(resourcePath) + "'";
    }

    private Regex GetMacroRegex(string source)
    {
        if (MacroRegexCache.TryGetValue(source, out var r))
            return r;

        var newRegex = new Regex(@"\b" + Regex.Escape(source) + @"\b");

        MacroRegexCache[source] = newRegex;

        return newRegex;
    }

    private IImmutableSourceFileDocument ApplyMacros(SourceFileDocument source)
    {
        var candidates = ConditionalSymbols.ToDictionary(
            x => x.Value,
            x => GetMacroRegex(x.Key)
        );

        var textual = source.CreateString();
        var nextApplications = candidates
            .SelectMany(x =>
                x.Value
                    .Matches(textual)
                    .Where(m => source.AllowReplace(m.Index, m.Length))
                    .Select(m => Tuple.Create(x.Key, m))
            )
            .ToList();

        if (nextApplications.Any())
        {
            foreach (var a in nextApplications.OrderByDescending(a => a.Item2.Index))
            {
                a.Item1.Apply(source, a.Item2);
            }
        }

        return source;
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

        if (context.directive() != null)
        {
            if (context.directive().GetText().StartsWith("include") ||
                context.directive().GetText().StartsWith("import"))
            {
                if (!_compilied)
                    return new EmptySourceFileDocument();

                return Visit(context.directive());
            }

            _compilied = Visit(context.directive()).AsLogical();
            directive = true;
        }

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

        return ApplyMacros(new SourceFileDocument(context.code().Select(Visit), ""));
    }

    public override IImmutableSourceFileDocument VisitCode([NotNull] DmeParser.CodeContext context)
    {
        if (context.@string() != null)
        {
            return Visit(context.@string());
        }

        if (context.resource() != null)
        {
            var src = context.resource();
            var resourceText = ApplyResourceMacros(src.GetText());
            return SourceFileDocument.Create(fileName, src.Start.Line, resourceText, true);
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

    public override IImmutableSourceFileDocument VisitString_contents_placeholder([NotNull] DmeParser.String_contents_placeholderContext context)
    {
        return SourceFileDocument.Create(fileName, context.Start.Line, "\"[]\"", true);
    }

    public override IImmutableSourceFileDocument VisitString_expression([NotNull] DmeParser.String_expressionContext context)
    {
        return Visit(context.code_block());
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

    public override IImmutableSourceFileDocument VisitString_contents_literal([NotNull] DmeParser.String_contents_literalContext context)
    {
        var contents = context.GetText();
        var contentLines = Regex.Split(contents, "\r\n|\r|\n").Select(EscapeString).ToArray();

        //add newline text inbetween
        for(var i = 0; i < contentLines.Length - 1; i++)
            contentLines[i] = contentLines[i] + "\\r\\n";

        return CreateStringConcat(
            context.Start.Line,
            contentLines
            .Where(x => x.Length > 0)
            .Select(c => 
                (IImmutableSourceFileDocument)SourceFileDocument.Create(fileName, context.Start.Line, "\"" + c + "\"", true)
            ).ToList()
        );
    }

    public override IImmutableSourceFileDocument VisitString([NotNull] DmeParser.StringContext context)
    {
        var contents = context.string_contents().Select(Visit).ToList();

        return CreateStringConcat(context.Start.Line, contents);
    }

    public override IImmutableSourceFileDocument VisitPreprocessorImport([NotNull] DmeParser.PreprocessorImportContext context)
    {
        var fileImport = context.directive_text().GetText().Trim();

        var libImport = false;

        if (fileImport.StartsWith("\"") && fileImport.EndsWith("\""))
            libImport = false;
        else if (fileImport.StartsWith("<") && fileImport.EndsWith(">"))
            libImport = true;
        else
            throw new Exception("Unrecognized file name quoting in import directive.");

        fileImport = fileImport.Substring(1, fileImport.Length - 2);

        var (newSymbols, importBody) = processImport(ConditionalSymbols.ToImmutableDictionary(),
            ResourceSearchDirectory.ToList(), libImport, fileImport);

        ConditionalSymbols = newSymbols.ToDictionary(x => x.Key, x => x.Value);

        if (importBody != null)
        {
            importBody = new ConcatSourceFileDocument(new IImmutableSourceFileDocument[]
            {
                importBody,
                SourceFileDocument.Create("generated", 1, "\r\n", false)
            },"");
            //importBody.Append("\r\n");
        }
        return importBody ?? new EmptySourceFileDocument();
    }

    public override SourceFileDocument VisitPreprocessorConditional([NotNull] DmeParser.PreprocessorConditionalContext context)
    {
        if (context.IF() != null)
        {
            var exprResult = Visit(context.preprocessor_expression()).AsLogical();
            _conditions.AddFirst(exprResult);
            return SourceFileDocument.CreateStatus(exprResult && IsCompiliedText());
        }

        if (context.ELIF() != null)
        {
            _conditions.RemoveFirst();
            var exprResult = Visit(context.preprocessor_expression()).AsLogical();
            _conditions.AddFirst(exprResult);
            return SourceFileDocument.CreateStatus(exprResult && IsCompiliedText());
        }

        if (context.ELSE() != null)
        {
            var val = _conditions.First.Value;
            _conditions.RemoveFirst();
            _conditions.AddFirst(!val);
            return SourceFileDocument.CreateStatus(val && IsCompiliedText());
        }

        _conditions.RemoveFirst();
        return SourceFileDocument.CreateStatus(_conditions.First.Value);
    }

    public override SourceFileDocument VisitPreprocessorDef([NotNull] DmeParser.PreprocessorDefContext context)
    {
        var conditionalSymbolText = context.CONDITIONAL_SYMBOL().GetText();
        if (context.IFDEF() != null || context.IFNDEF() != null)
        {
            var condition = ConditionalSymbols.ContainsKey(conditionalSymbolText);
            if (context.IFNDEF() != null) condition = !condition;
            _conditions.AddFirst(condition);
            return SourceFileDocument.CreateStatus(condition  && IsCompiliedText());
        }

        if (IsCompiliedText()) ConditionalSymbols.Remove(conditionalSymbolText);
        return SourceFileDocument.CreateStatus(IsCompiliedText());
    }

    public override SourceFileDocument VisitPreprocessorPragma([NotNull] DmeParser.PreprocessorPragmaContext context)
    {
        return SourceFileDocument.CreateStatus(IsCompiliedText());
    }

    public override SourceFileDocument VisitPreprocessorError([NotNull] DmeParser.PreprocessorErrorContext context)
    {
        return SourceFileDocument.CreateStatus(IsCompiliedText());
    }

    public override SourceFileDocument VisitPreprocessorDefine([NotNull] DmeParser.PreprocessorDefineContext context)
    {
        if (IsCompiliedText())
        {
            var str = new StringBuilder();

            if (context.directive_text() != null)
                foreach (DmeParser.Directive_textContext d in new[] { context.directive_text() })
                    str.Append(d.GetText() != null ? d.GetText() : "\r\n");

            var directiveText = str.ToString().Trim();
            var directiveName = context.CONDITIONAL_SYMBOL().GetText().Replace(" ", "");

            DefineSymbol(directiveName, directiveText);
        }

        return SourceFileDocument.CreateStatus(IsCompiliedText());
    }

    private void DefineSymbol(string directiveName, string directiveText)
    {
        string[]? argList = null;
        if (directiveName.Contains('(') || directiveName.Contains(')'))
        {
            if (directiveName.IndexOf('(') > directiveName.IndexOf(')'))
                throw new Exception("Invalid argument list.");

            var arglistBegin = directiveName.IndexOf('(') + 1;
            var arglistEnd = directiveName.IndexOf(')');

            var strArglist = directiveName.Substring(
                arglistBegin,
                arglistEnd - arglistBegin);

            argList = strArglist.Split(',', StringSplitOptions.TrimEntries);

            if (argList.Any(x => x.Length == 0))
                throw new Exception("Arguments cannot be empty.");

            if (argList.Distinct().Count() != argList.Length)
                throw new Exception("Argument names must be distinct.");

            directiveName = directiveName.Substring(0, directiveName.IndexOf('('));
        }

        if (directiveName == "FILE_DIR") resourceDirectories.Add(Path.Join(resourcePathBase, directiveText));

        ConditionalSymbols[directiveName] = new MacroDefinition(directiveText, argList);
    }

    public override SourceFileDocument VisitPreprocessorConstant([NotNull] DmeParser.PreprocessorConstantContext context)
    {
        if (context.TRUE() != null || context.FALSE() != null)
            return SourceFileDocument.CreateStatus(context.TRUE() != null);
        return SourceFileDocument.Create(fileName, context.Start.Line, context.GetText(), false);
    }

    public override SourceFileDocument VisitPreprocessorConditionalSymbol(
        [NotNull] DmeParser.PreprocessorConditionalSymbolContext context)
    {
        if (ConditionalSymbols.TryGetValue(context.CONDITIONAL_SYMBOL().GetText(), out var symbol))
            return SourceFileDocument.Create(fileName, context.start.Line, symbol.Text, false);

        return SourceFileDocument.CreateStatus(false);
    }

    public override IImmutableSourceFileDocument VisitPreprocessorParenthesis([NotNull] DmeParser.PreprocessorParenthesisContext context)
    {
        return Visit(context.preprocessor_expression());
    }

    public override SourceFileDocument VisitPreprocessorNot([NotNull] DmeParser.PreprocessorNotContext context)
    {
        return SourceFileDocument.CreateStatus(!(Visit(context.preprocessor_expression())).AsLogical());
    }

    public override SourceFileDocument VisitPreprocessorBinary([NotNull] DmeParser.PreprocessorBinaryContext context)
    {
        var expr1Result = Visit(context.preprocessor_expression(0));//.AsLogical();
        var expr2Result = Visit(context.preprocessor_expression(1));//.AsLogical();
        var op = context.op.Text;
        var result = false;
        switch (op)
        {
            case "&&":
                result = expr1Result.AsLogical() && expr2Result.AsLogical();
                break;
            case "||":
                result = expr1Result.AsLogical() || expr2Result.AsLogical();
                break;
            case "==":
                result = expr1Result.AsLogical() == expr2Result.AsLogical();
                break;
            case "!=":
                result = expr1Result.AsLogical() != expr2Result.AsLogical();
                break;
            case "<":
            case ">":
            case "<=":
            case ">=":
                result = false;
                if (expr1Result.TryNumeric(out var x1) && expr2Result.TryNumeric(out var x2))
                {
                    switch (op)
                    {
                        case "<":
                            result = x1 < x2;
                            break;
                        case ">":
                            result = x1 > x2;
                            break;
                        case "<=":
                            result = x1 <= x2;
                            break;
                        case ">=":
                            result = x1 >= x2;
                            break;
                    }
                }

                break;
            default:
                result = true;
                break;
        }

        return SourceFileDocument.CreateStatus(result);
    }

    public override SourceFileDocument VisitPreprocessorDefined([NotNull] DmeParser.PreprocessorDefinedContext context)
    {
        return SourceFileDocument.CreateStatus(ConditionalSymbols.ContainsKey(context.CONDITIONAL_SYMBOL().GetText()));
    }

    private bool IsCompiliedText()
    {
        return !_conditions.Contains(false);
    }
}