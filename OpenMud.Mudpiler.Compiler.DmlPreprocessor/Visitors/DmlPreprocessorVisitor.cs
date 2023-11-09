using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using static System.Net.Mime.MediaTypeNames;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

public delegate (IImmutableDictionary<string, MacroDefinition> macros, SourceFileDocument importBody) ProcessImport(
    IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib, string fileName);

public delegate string ResolveResourceDirectory(List<string> knownFileDirs, string path);

internal class DmlPreprocessorVisitor : DmeParserBaseVisitor<SourceFileDocument>
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

    private string ApplyResourceMacros(string source)
    {
        var path = source.Trim('\'');

        var resourcePath = resolveResourceDirectory(ResourceSearchDirectory.ToList(), path);
        resourcePath = NormalizeResourcePath(Path.GetRelativePath(resourcePathBase, resourcePath));

        return "'" + resourcePath + "'";
    }



    private SourceFileDocument ApplyMacros(SourceFileDocument source)
    {
        var candidates = ConditionalSymbols.ToDictionary(
            x => x.Value,
            x => new Regex(@"\b" + Regex.Escape(x.Key) + @"\b")
        );

        while (true)
        {

            var nextApplications = candidates
                .SelectMany(x =>
                    x.Value
                        .Matches(source.Textual)
                        .Where(m => source.AllowReplace(m.Index, m.Length))
                        .Select(m => Tuple.Create(x.Key, m))
                )
                .ToList();

            if (!nextApplications.Any())
                break;

            foreach (var a in nextApplications.OrderByDescending(a => a.Item2.Index))
            {
                a.Item1.Apply(source, a.Item2);
            }
        }

        return source;
    }

    public override SourceFileDocument VisitDmlDocument([NotNull] DmeParser.DmlDocumentContext context)
    {
        var sb = new List<SourceFileDocument>();

        foreach (DmeParser.TextContext text in context.text())
            sb.Add(Visit(text));

        return new SourceFileDocument(sb);
    }

    public override SourceFileDocument VisitText([NotNull] DmeParser.TextContext context)
    {
        var result = SourceFileDocument.Create(fileName, context.Start.Line, _tokensStream.GetText(context), false);

        var directive = false;

        if (context.directive() != null)
        {
            if (context.directive().GetText().StartsWith("include") ||
                context.directive().GetText().StartsWith("import"))
                return Visit(context.directive());// + "\r\n";

            _compilied = Visit(context.directive()).AsLogical();
            directive = true;
        }

        if (!_compilied || directive)
        {
            result = SourceFileDocument.Empty;
        }

        if (_compilied && !directive)
        {
            //In this case, it is not a macro and we are compiling it.
            var cb = context.code_block();

            if (cb == null)
                result = SourceFileDocument.Empty;
            else
                result = Visit(cb);
        }

        return result;
    }

    public override SourceFileDocument VisitCode_block([NotNull] DmeParser.Code_blockContext context)
    {
        //Apply macros here
        var r = new SourceFileDocument(context.code().Select(Visit), "");

        return ApplyMacros(r);
    }

    public override SourceFileDocument VisitCode([NotNull] DmeParser.CodeContext context)
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

            var document = new SourceFileDocument(pieces, "");

            return document;
        }

        throw new Exception("Unknown code type");
    }

    public override SourceFileDocument VisitString_contents_placeholder([NotNull] DmeParser.String_contents_placeholderContext context)
    {
        return SourceFileDocument.Create(fileName, context.Start.Line, "\"[]\"", true);
    }

    public override SourceFileDocument VisitString_expression([NotNull] DmeParser.String_expressionContext context)
    {
        return Visit(context.code_block());
    }

    private SourceFileDocument CreateStringConcat(int line, List<SourceFileDocument> contents)
    {
        if (contents.Count == 0)
            return SourceFileDocument.Create(fileName, line, $"\"\"", true);

        if (contents.Count > 1)
        {
            var components = new List<SourceFileDocument>();
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

            return new SourceFileDocument(components, "");
        }

        return contents.Single();
    }

    public override SourceFileDocument VisitString_contents_literal([NotNull] DmeParser.String_contents_literalContext context)
    {
        var contents = context.GetText();
        var contentLines = Regex.Split(contents, "\r\n|\r|\n");

        //add newline text inbetween
        for(var i = 0; i < contentLines.Length - 2; i++)
            contentLines[i] = contentLines[i] + "\\r\\n";

        return CreateStringConcat(
            context.Start.Line,
            contentLines
            .Where(x => x.Length > 0)
            .Select(c => 
                SourceFileDocument.Create(fileName, context.Start.Line, "\"" + c + "\"", true)
            ).ToList()
        );
    }

    public override SourceFileDocument VisitString([NotNull] DmeParser.StringContext context)
    {
        var contents = context.string_contents().Select(Visit).ToList();

        return CreateStringConcat(context.Start.Line, contents);
    }

    public override SourceFileDocument VisitPreprocessorImport([NotNull] DmeParser.PreprocessorImportContext context)
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

        return importBody;
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

    public override SourceFileDocument VisitPreprocessorParenthesis([NotNull] DmeParser.PreprocessorParenthesisContext context)
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