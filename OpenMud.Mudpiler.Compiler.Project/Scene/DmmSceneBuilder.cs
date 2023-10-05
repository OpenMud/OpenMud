using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmmGrammar;
using OpenMud.Mudpiler.Compiler.Project.Scene.DmmVisitors;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Compiler.Project.Scene;

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

public class DmmSceneBuilderFactory
{
    private readonly IMudEntityBuilder entityBuilder;

    public DmmSceneBuilderFactory(IMudEntityBuilder entityBuilder)
    {
        this.entityBuilder = entityBuilder;
    }

    public IMudSceneBuilder Build(string dmmSource)
    {
        var errorListener = new ErrorListener();

        var inputStream = new AntlrInputStream(dmmSource);
        var lexer = new DmmLexer(inputStream);
        lexer.AddErrorListener(errorListener);
        var commonTokenStream = new CommonTokenStream(lexer);

        var parser = new DmmParser(commonTokenStream);
        parser.AddErrorListener(errorListener);

        var ctx = parser.dmm_module();
        var visitor = new DmmLibraryBuilder();

        if (errorListener.Errors.Any())
            throw new Exception(string.Join("\n", errorListener.Errors));

        var typeLibrary = new DmmLibraryBuilder().Visit(ctx);

        var typeKeyLength = typeLibrary.Keys.Select(x => x.Length).Distinct().SingleOrDefault(-1);

        var mapDecl = new DmmMapBuilder(typeKeyLength).Visit(ctx);

        var unpacked = mapDecl.ToDictionary(x => x.Key, x => typeLibrary[x.Value]);

        var boundsX = unpacked.Select(x => x.Key.Item1).Max() + 1;
        var boundsY = unpacked.Select(x => x.Key.Item2).Max() + 1;

        var logicGuids = new Dictionary<string, Guid>();

        return new SimpleSceneBuilder(new WorldBounds(boundsX, boundsY), World =>
        {
            foreach (var (location, classList) in unpacked)
            foreach (var cls in classList)
            {
                var e = World.CreateEntity();

                var initArgs = new Dictionary<string, object>();

                foreach (var (k, v) in cls.IntParameters)
                    initArgs[k] = (object)v;

                foreach (var (k, v) in cls.StringParameters)
                    initArgs[k] = (object)v;

                if (initArgs.Any())
                    e.Set(new LogicFieldInitializerComponent(initArgs));

                entityBuilder.CreateAtomic(e, cls.ClassName, null, location.Item1, location.Item2);

                //Area pieces always share the same logic instance.
                if (RuntimeTypeResolver.InheritsBaseTypeDatum(cls.ClassName, DmlPrimitiveBaseType.Area))
                {
                    var clsNormal = DmlPath.NormalizeClassName(cls.ClassName);
                    if (!logicGuids.ContainsKey(DmlPath.NormalizeClassName(clsNormal)))
                        logicGuids[clsNormal] = Guid.NewGuid();

                    e.Set(new LogicIdentifierComponent(logicGuids[clsNormal]));
                }
            }
        });
    }
}