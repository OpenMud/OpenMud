using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmeGrammar;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;
using static System.Net.Mime.MediaTypeNames;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor;

public class ErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{
    public readonly List<string> Errors = new();

    public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] int offendingSymbol, int line,
        int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
    {
        Errors.Add($"{line}:{charPositionInLine} {msg}");
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
        ResolveResourceDirectory? resolveResourceDirectory, ProcessImport? processImport,
        IImmutableDictionary<string, MacroDefinition>? predefined = null)
    {
        return Preprocess(fileName, resourcePathBase, Enumerable.Empty<string>(), text, resolveResourceDirectory ?? nullResolveDirectory, processImport ?? nullResolveImport,
            out var _, predefined);
    }

    private static (IImmutableDictionary<string, MacroDefinition> macros, IImmutableSourceFileDocument importBody) nullResolveImport(IImmutableDictionary<string, MacroDefinition> dict, List<string> resourceDirectories, bool isLib, string fileName)
    {
        throw new NotImplementedException();
    }

    private static string nullResolveDirectory(List<string> knownFileDirs, string path)
    {
        throw new NotImplementedException();
    }

    private static IImmutableSourceFileDocument ExecuteMacroPreprocessPass(string filePath, string resourcePathBase, IEnumerable<string> resourceDirectory, string text,
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
        var visitor = new DmlMacroVisitor(filePath, resourcePathBase, resourceDirectory, commonTokenStream,
            resolveResourceDirectory, processImport, predefined);

        var allErrors = errorListener.Errors.ToList();

        if (allErrors.Any())
            throw new Exception(string.Join("\n", allErrors));

        var r = visitor.Visit(ctx);

        resultantDefinitions = visitor.MacroDefinitions;

        return r;
    }


    public static IImmutableSourceFileDocument PreprocessAsDocument(string filePath, string resourcePathBase, IEnumerable<string> resourceDirectory, string text,
        ResolveResourceDirectory resolveResourceDirectory, ProcessImport processImport,
        out IImmutableDictionary<string, MacroDefinition> resultantDefinitions,
        IImmutableDictionary<string, MacroDefinition>? predefined = null)
    {
        var r = Preprocess(filePath, resourcePathBase, resourceDirectory, text, resolveResourceDirectory, processImport, out resultantDefinitions, predefined, false);

        return SourceFileDocument.Create(filePath, 1, r, false);
    }

    private static string PreprocessText(string filePath, string text)
    {
        var errorListener = new ErrorListener();

        var inputStream = new AntlrInputStream(text);
        var lexer = new DmeLexer(inputStream);
        lexer.AddErrorListener(errorListener);
        var commonTokenStream = new CommonTokenStream(lexer);

        var parser = new DmeParser(commonTokenStream);
        parser.AddErrorListener(errorListener);


        var ctx = parser.dmlDocument();
        var visitor = new DmlTextProcessingVisitor(filePath, commonTokenStream);

        var allErrors = errorListener.Errors.ToList();

        if (allErrors.Any())
            throw new Exception(string.Join("\n", allErrors));

        var r = visitor.Visit(ctx);

        return r.AsPlainText(false);
    }

    public static string Preprocess(string filePath, string resourcePathBase, IEnumerable<string> resourceDirectory, string text,
        ResolveResourceDirectory resolveResourceDirectory, ProcessImport processImport,
        out IImmutableDictionary<string, MacroDefinition> resultantDefinitions,
        IImmutableDictionary<string, MacroDefinition>? predefined = null, bool injectLineNumbers = false)
    {
        var r = ExecuteMacroPreprocessPass(
            filePath,
            resourcePathBase,
            resourceDirectory,
            text,
            resolveResourceDirectory,
            processImport,
            out resultantDefinitions,
            predefined
        );

        var macroProcessed = r.AsPlainText(injectLineNumbers);

        var result = PreprocessText(filePath, macroProcessed);

        return result;
    }
}