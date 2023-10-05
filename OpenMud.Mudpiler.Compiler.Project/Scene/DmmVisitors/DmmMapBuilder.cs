using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmmGrammar;

namespace OpenMud.Mudpiler.Compiler.Project.Scene.DmmVisitors;

internal class DmmMapBuilder : DmmParserBaseVisitor<Dictionary<Tuple<int, int>, string>>
{
    private readonly int typeKeyLength;

    public DmmMapBuilder(int typeKeyLength)
    {
        this.typeKeyLength = typeKeyLength;
    }

    public override Dictionary<Tuple<int, int>, string> VisitMapdecl([NotNull] DmmParser.MapdeclContext context)
    {
        var mapDecl = new Dictionary<Tuple<int, int>, string>();

        var rows = context.STRING().GetText().Trim('"').Split(new[] { '\r', '\n' }).Select(x => x.Trim())
            .Where(x => x.Length > 0);

        var (originX, originY) = (int.Parse(context.x.Text), int.Parse(context.y.Text));

        foreach (var rowText in rows)
        {
            for (var x = 0; x < rowText.Length; x += typeKeyLength)
                mapDecl[Tuple.Create(x / typeKeyLength + originX, originY)] =
                    rowText.Substring(x, typeKeyLength);
            originY++;
        }

        return mapDecl;
    }

    public override Dictionary<Tuple<int, int>, string> VisitDmm_module([NotNull] DmmParser.Dmm_moduleContext context)
    {
        var charCodes = new Dictionary<Tuple<int, int>, string>();

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