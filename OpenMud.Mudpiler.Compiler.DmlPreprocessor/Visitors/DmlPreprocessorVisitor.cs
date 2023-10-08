using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

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

    private SourceFileDocument ApplyResourceMacros(SourceFileDocument source)
    {
        var processedOrigins = new HashSet<int>();
        while (true)
        {
            var unpacked = source.Unpack();
            var allowMatch = Blackout.CreateBlackouts(unpacked.Contents, false);

            var nextApplication = resourceRegex
                .Matches(unpacked.Contents)
                .OrderBy(m => m.Index)
                .Where(m => !processedOrigins.Contains(m.Index))
                .Where(allowMatch.Allow)
                .FirstOrDefault();

            if (nextApplication == null)
                break;

            processedOrigins.Add(nextApplication.Index);
            var path = nextApplication.Value.Trim('\'');

            var newValue = "'" +
                           NormalizeResourcePath(resolveResourceDirectory(ResourceSearchDirectory.ToList(), path)) +
                           "'";

            source = unpacked.ReplaceAndPack(nextApplication, newValue);
        }

        return source;
    }



    private SourceFileDocument ApplyMacros(SourceFileDocument source)
    {
        var candidates = ConditionalSymbols.ToDictionary(
            x => x.Value,
            x => new Regex(@"\b" + Regex.Escape(x.Key) + @"\b")
        );

        while (true)
        {
            var unpacked = source.Unpack();
            var allowMatch = Blackout.CreateBlackouts(unpacked.Contents);

            var nextApplication = candidates
                .SelectMany(x =>
                    x.Value
                        .Matches(unpacked.Contents)
                        .Where(allowMatch.Allow)
                        .Select(m => Tuple.Create(x.Key, m))
                )
                .FirstOrDefault();

            if (nextApplication == null)
                break;

            source = nextApplication.Item1.Apply(unpacked, nextApplication.Item2);
        }

        source = ApplyResourceMacros(source);

        return source;
    }

    public override SourceFileDocument VisitDmlDocument([NotNull] DmeParser.DmlDocumentContext context)
    {
        var sb = new List<SourceFileDocument>();

        foreach (DmeParser.TextContext text in context.text())
            sb.Add(Visit(text));

        return new SourceFileDocument(sb.SelectMany(s => s.Contents));
    }

    public override SourceFileDocument VisitText([NotNull] DmeParser.TextContext context)
    {
        var result = SourceFileDocument.Create(fileName, context.Start.Line, _tokensStream.GetText(context));

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
            //var sb = new StringBuilder(result.Length);
            //foreach (var c in result) sb.Append(c == '\r' || c == '\n' ? c : ' ');

            result = new SourceFileDocument(Enumerable.Empty<SourceFileLine>()); //sb.ToString();
        }

        if (_compilied && !directive) result = ApplyMacros(result);


        return result;
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
        return SourceFileDocument.Create(fileName, context.Start.Line, context.GetText());
    }

    public override SourceFileDocument VisitPreprocessorConditionalSymbol(
        [NotNull] DmeParser.PreprocessorConditionalSymbolContext context)
    {
        if (ConditionalSymbols.TryGetValue(context.CONDITIONAL_SYMBOL().GetText(), out var symbol))
            return SourceFileDocument.Create(fileName, context.start.Line, symbol.Text);

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