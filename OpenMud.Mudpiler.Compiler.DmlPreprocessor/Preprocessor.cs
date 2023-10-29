using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor;

public class ErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{
    public readonly List<string> Errors = new();

    public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] int offendingSymbol, int line,
        int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
    {
        Errors.Add(e.Message);
    }

    public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line,
        int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
    {
        Errors.Add($"{line}:{charPositionInLine} {offendingSymbol}");
    }
}

public class Preprocessor
{
    public static string Preprocess(string fileName, string resourcePathBase, string text,
        ResolveResourceDirectory resolveResourceDirectory, ProcessImport processImport,
        IImmutableDictionary<string, MacroDefinition>? predefined = null)
    {
        return Preprocess(fileName, resourcePathBase, Enumerable.Empty<string>(), text, resolveResourceDirectory, processImport,
            out var _, predefined);
    }

    public static SourceFileDocument PreprocessAsDocument(string filePath, string resourcePathBase, IEnumerable<string> resourceDirectory, string text,
        ResolveResourceDirectory resolveResourceDirectory, ProcessImport processImport,
        out IImmutableDictionary<string, MacroDefinition> resultantDefinitions,
        IImmutableDictionary<string, MacroDefinition>? predefined = null)
    {
        if (predefined == null)
            predefined = new Dictionary<string, MacroDefinition>().ToImmutableDictionary();

        var errorListener = new ErrorListener();

        var inputStream = new AntlrInputStream(text);
        var lexer = new DmeLexer(inputStream);
        lexer.AddErrorListener(errorListener);
        var commonTokenStream = new CommonTokenStream(lexer);

        var parser = new DmeParser(commonTokenStream);
        parser.AddErrorListener(errorListener);


        var ctx = parser.dmlDocument();
        var visitor = new DmlPreprocessorVisitor(filePath, resourcePathBase, resourceDirectory, commonTokenStream,
            resolveResourceDirectory, processImport, predefined);

        var allErrors = errorListener.Errors.ToList();

        if (allErrors.Any())
            throw new Exception(string.Join("\n", allErrors));

        var r = visitor.Visit(ctx);

        resultantDefinitions = visitor.MacroDefinitions;

        return r;
    }


    public static string Preprocess(string filePath, string resourcePathBase, IEnumerable<string> resourceDirectory, string text,
        ResolveResourceDirectory resolveResourceDirectory, ProcessImport processImport,
        out IImmutableDictionary<string, MacroDefinition> resultantDefinitions,
        IImmutableDictionary<string, MacroDefinition>? predefined = null)
    {
        var r = PreprocessAsDocument(
            filePath,
            resourcePathBase,
            resourceDirectory,
            text,
            resolveResourceDirectory,
            processImport,
            out resultantDefinitions,
            predefined
        );

        return r.AsPlainText();
    }
}