using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmmGrammar;

namespace OpenMud.Mudpiler.Compiler.Project.Scene.DmmVisitors;

public struct DmmTypeInit
{
    public readonly string ClassName;
    public readonly Dictionary<string, string> StringParameters = new();
    public readonly Dictionary<string, int> IntParameters = new();

    public DmmTypeInit(string className)
    {
        ClassName = className;
    }
}

public class DmmLibraryBuilder : DmmParserBaseVisitor<Dictionary<string, List<DmmTypeInit>>>
{
    public override Dictionary<string, List<DmmTypeInit>> VisitMap_piece_decl(
        [NotNull] DmmParser.Map_piece_declContext context)
    {
        var name = context.id.Text.Trim('"');
        var types = context.typelist().typedecl().Select(x =>
        {
            var t = new DmmTypeInit(x.name.GetText());

            if (x.typeinit() != null)
                foreach (var arg in x.typeinit().fieldinit())
                {
                    var name = arg.NAME().GetText();
                    var initExpr = arg.fieldinitexpr();

                    if (initExpr.INT() != null)
                        t.IntParameters.Add(name, int.Parse(initExpr.INT().GetText()));
                    else
                        t.StringParameters.Add(name, initExpr.STRING().GetText().Trim('"'));
                }

            return t;
        });

        return new Dictionary<string, List<DmmTypeInit>> { { name, types.ToList() } };
    }

    public override Dictionary<string, List<DmmTypeInit>> VisitDmm_module([NotNull] DmmParser.Dmm_moduleContext context)
    {
        var charCodes = new Dictionary<string, List<DmmTypeInit>>();

        foreach (var s in context.stmt())
        {
            var r = Visit(s);

            if (r != null)
                foreach (var (c, l) in r)
                    charCodes[c] = l;
        }

        return charCodes;
    }
}