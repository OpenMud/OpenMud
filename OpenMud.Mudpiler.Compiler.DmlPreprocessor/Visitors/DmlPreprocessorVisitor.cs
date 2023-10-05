using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

public delegate (IImmutableDictionary<string, MacroDefinition> macros, string importBody) ProcessImport(
    IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib, string fileName);

public delegate string ResolveResourceDirectory(List<string> knownFileDirs, string path);

internal class DmlPreprocessorVisitor : DmeParserBaseVisitor<string>
{
    private static readonly Regex resourceRegex = new(@"'[^'\r\n]*'");

    private readonly ProcessImport processImport;
    private readonly ResolveResourceDirectory resolveResourceDirectory;
    private readonly List<string> resourceDirectories = new();
    private readonly string resourcePathBase;
    private bool _compilied = true;
    private readonly LinkedList<bool> _conditions = new();
    private readonly CommonTokenStream _tokensStream;

    private Dictionary<string, MacroDefinition> ConditionalSymbols = new();

    public DmlPreprocessorVisitor(string resourcePathBase, IEnumerable<string> resourceDirectory,
        CommonTokenStream commonTokenStream, ResolveResourceDirectory resolveResourceDirectory,
        ProcessImport processImport, IImmutableDictionary<string, MacroDefinition> macros) :
        this(resourcePathBase, resourceDirectory, commonTokenStream, resolveResourceDirectory, processImport)
    {
        ConditionalSymbols = macros.ToDictionary(x => x.Key, x => x.Value);
    }

    public DmlPreprocessorVisitor(string resourcePathBase, IEnumerable<string> resourceDirectory,
        CommonTokenStream commonTokenStream, ResolveResourceDirectory resolveResourceDirectory,
        ProcessImport processImport)
    {
        resourceDirectories = resourceDirectory.ToList();
        this.resourcePathBase = resourcePathBase;
        _conditions.AddFirst(true);
        _tokensStream = commonTokenStream;
        this.processImport = processImport;
        this.resolveResourceDirectory = resolveResourceDirectory;
    }

    public IImmutableDictionary<string, MacroDefinition> MacroDefinitions => ConditionalSymbols.ToImmutableDictionary();
    public IEnumerable<string> ResourceSearchDirectory => resourceDirectories;

    private Func<Match, bool> CreateCommentBlackouts(string source, bool blackoutResources)
    {
        //Sometimes just writing a parser is simpler than trying to get Antlr to do what you want it to.
        List<Tuple<int, int>> blackouts = new();

        var multilineCommentDepth = 0;
        var isInString = false;
        var isInResource = false;
        var isInSingleLineComment = false;

        var start = 0;

        void EndOfBlackout(int idx)
        {
            blackouts.Add(Tuple.Create(start, idx));
            start = 0;
        }

        for (var i = 0; i < source.Length; i++)
        {
            var remaining = source.Length - i - 1;

            bool acceptAndSkip(string v)
            {
                if (remaining < v.Length)
                    return false;

                for (var w = 0; w < v.Length; w++)
                    if (source[w + i] != v[w])
                        return false;

                i += v.Length - 1;
                return true;
            }

            if (multilineCommentDepth > 0)
            {
                if (acceptAndSkip("*/"))
                {
                    multilineCommentDepth--;

                    if (multilineCommentDepth == 0)
                        EndOfBlackout(i);
                    continue;
                }

                if (acceptAndSkip("/*")) multilineCommentDepth++;
            }
            else if (isInString)
            {
                if (acceptAndSkip("\\\""))
                    continue;

                if (acceptAndSkip("\""))
                {
                    isInString = false;
                    EndOfBlackout(i);
                }
            }
            else if (isInResource)
            {
                if (acceptAndSkip("\\\'"))
                    continue;

                if (acceptAndSkip("'"))
                {
                    if (blackoutResources)
                        EndOfBlackout(i);
                    isInResource = false;
                }
            }
            else if (isInSingleLineComment)
            {
                if (acceptAndSkip("\r\n") || acceptAndSkip("\n") || acceptAndSkip("\r"))
                {
                    isInSingleLineComment = false;
                    EndOfBlackout(i);
                }
            }
            else
            {
                var startBuffer = i;
                if (acceptAndSkip("//"))
                {
                    isInSingleLineComment = true;
                    start = startBuffer;
                }
                else if (acceptAndSkip("/*"))
                {
                    multilineCommentDepth++;
                    start = startBuffer;
                }
                else if (acceptAndSkip("\""))
                {
                    isInString = true;
                    start = startBuffer;
                }
                else if (acceptAndSkip("'"))
                {
                    isInResource = true;

                    if (blackoutResources)
                        start = startBuffer;
                }
            }
        }

        return m =>
        {
            return !Enumerable.Range(m.Index, m.Length).Any(
                idx => blackouts.Any(blk => idx >= blk.Item1 && idx <= blk.Item2)
            );
        };
    }

    public Func<Match, bool> CreateBlackouts(string source, bool blackoutResources = true)
    {
        return CreateCommentBlackouts(source, blackoutResources);
    }

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
        var processedOrigins = new HashSet<int>();
        while (true)
        {
            var allowMatch = CreateBlackouts(source, false);

            var nextApplication = resourceRegex
                .Matches(source)
                .OrderBy(m => m.Index)
                .Where(m => !processedOrigins.Contains(m.Index))
                .Where(allowMatch)
                .FirstOrDefault();

            if (nextApplication == null)
                break;

            processedOrigins.Add(nextApplication.Index);
            var path = nextApplication.Value.Trim('\'');

            var newValue = "'" +
                           NormalizeResourcePath(resolveResourceDirectory(ResourceSearchDirectory.ToList(), path)) +
                           "'";

            source = source.Substring(0, nextApplication.Index) + newValue +
                     source.Substring(nextApplication.Index + nextApplication.Length);
        }

        return source;
    }

    private string ApplyMacros(string source)
    {
        var candidates = ConditionalSymbols.ToDictionary(
            x => x.Value,
            x => new Regex(@"\b" + Regex.Escape(x.Key) + @"\b")
        );

        while (true)
        {
            var allowMatch = CreateBlackouts(source);

            var nextApplication = candidates
                .SelectMany(x =>
                    x.Value
                        .Matches(source)
                        .Where(allowMatch)
                        .Select(m => Tuple.Create(x.Key, m))
                )
                .FirstOrDefault();

            if (nextApplication == null)
                break;

            source = nextApplication.Item1.Apply(source, nextApplication.Item2);
        }

        source = ApplyResourceMacros(source);

        return source;
    }

    public override string VisitDmlDocument([NotNull] DmeParser.DmlDocumentContext context)
    {
        var sb = new StringBuilder();
        foreach (DmeParser.TextContext text in context.text())
            sb.Append(Visit(text));

        return sb.ToString();
    }

    public override string VisitText([NotNull] DmeParser.TextContext context)
    {
        var result = _tokensStream.GetText(context);

        var directive = false;

        if (context.directive() != null)
        {
            if (context.directive().GetText().StartsWith("include") ||
                context.directive().GetText().StartsWith("import"))
                return Visit(context.directive()) + "\r\n";

            _compilied = Visit(context.directive()) == true.ToString();
            directive = true;
        }

        if (!_compilied || directive)
        {
            var sb = new StringBuilder(result.Length);
            foreach (var c in result) sb.Append(c == '\r' || c == '\n' ? c : ' ');

            result = sb.ToString();
        }

        if (_compilied && !directive) result = ApplyMacros(result);


        return result;
    }


    public override string VisitPreprocessorImport([NotNull] DmeParser.PreprocessorImportContext context)
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

    public override string VisitPreprocessorConditional([NotNull] DmeParser.PreprocessorConditionalContext context)
    {
        if (context.IF() != null)
        {
            var exprResult = Visit(context.preprocessor_expression()) == true.ToString();
            _conditions.AddFirst(exprResult);
            return (exprResult && IsCompiliedText()).ToString();
        }

        if (context.ELIF() != null)
        {
            _conditions.RemoveFirst();
            var exprResult = Visit(context.preprocessor_expression()) == true.ToString();
            _conditions.AddFirst(exprResult);
            return (exprResult && IsCompiliedText()).ToString();
        }

        if (context.ELSE() != null)
        {
            var val = _conditions.First.Value;
            _conditions.RemoveFirst();
            _conditions.AddFirst(!val);
            return (!val ? IsCompiliedText() : false).ToString();
        }

        _conditions.RemoveFirst();
        return _conditions.First.Value.ToString();
    }

    public override string VisitPreprocessorDef([NotNull] DmeParser.PreprocessorDefContext context)
    {
        var conditionalSymbolText = context.CONDITIONAL_SYMBOL().GetText();
        if (context.IFDEF() != null || context.IFNDEF() != null)
        {
            var condition = ConditionalSymbols.ContainsKey(conditionalSymbolText);
            if (context.IFNDEF() != null) condition = !condition;
            _conditions.AddFirst(condition);
            return (condition && IsCompiliedText()).ToString();
        }

        if (IsCompiliedText()) ConditionalSymbols.Remove(conditionalSymbolText);
        return IsCompiliedText().ToString();
    }

    public override string VisitPreprocessorPragma([NotNull] DmeParser.PreprocessorPragmaContext context)
    {
        return IsCompiliedText().ToString();
    }

    public override string VisitPreprocessorError([NotNull] DmeParser.PreprocessorErrorContext context)
    {
        return IsCompiliedText().ToString();
    }

    public override string VisitPreprocessorDefine([NotNull] DmeParser.PreprocessorDefineContext context)
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

        return IsCompiliedText().ToString();
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

    public override string VisitPreprocessorConstant([NotNull] DmeParser.PreprocessorConstantContext context)
    {
        if (context.TRUE() != null || context.FALSE() != null)
            return (context.TRUE() != null).ToString();
        return context.GetText();
    }

    public override string VisitPreprocessorConditionalSymbol(
        [NotNull] DmeParser.PreprocessorConditionalSymbolContext context)
    {
        if (ConditionalSymbols.TryGetValue(context.CONDITIONAL_SYMBOL().GetText(), out var symbol))
            return symbol.Text;
        return false.ToString();
    }

    public override string VisitPreprocessorParenthesis([NotNull] DmeParser.PreprocessorParenthesisContext context)
    {
        return Visit(context.preprocessor_expression());
    }

    public override string VisitPreprocessorNot([NotNull] DmeParser.PreprocessorNotContext context)
    {
        return (!bool.Parse(Visit(context.preprocessor_expression()))).ToString();
    }

    public override string VisitPreprocessorBinary([NotNull] DmeParser.PreprocessorBinaryContext context)
    {
        var expr1Result = Visit(context.preprocessor_expression(0));
        var expr2Result = Visit(context.preprocessor_expression(1));
        var op = context.op.Text;
        var result = false;
        switch (op)
        {
            case "&&":
                result = expr1Result == true.ToString() && expr2Result == true.ToString();
                break;
            case "||":
                result = expr1Result == true.ToString() || expr2Result == true.ToString();
                break;
            case "==":
                result = expr1Result == expr2Result;
                break;
            case "!=":
                result = expr1Result != expr2Result;
                break;
            case "<":
            case ">":
            case "<=":
            case ">=":
                int x1, x2;
                result = false;
                if (int.TryParse(expr1Result, out var _) && int.TryParse(expr2Result, out var _))
                {
                    x1 = int.Parse(expr1Result);
                    x2 = int.Parse(expr2Result);
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

        return result.ToString();
    }

    public override string VisitPreprocessorDefined([NotNull] DmeParser.PreprocessorDefinedContext context)
    {
        return ConditionalSymbols.ContainsKey(context.CONDITIONAL_SYMBOL().GetText()).ToString();
    }

    private bool IsCompiliedText()
    {
        return !_conditions.Contains(false);
    }
}