namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.PrebuildProcessor;

public class FieldDefaultAssignments : IClassPrebuildProcessor
{
    private readonly string[] atomicFieldInit =
    {
        "/mob",
        "/obj",
        "/area",
        "/turf",
        "/atom"
    };

    public void Process(string fullName, IDreamMakerSymbolResolver cls)
    {
        /*
            var rootedPath = DmlPath.RootClassName(fullName);

            var hasAtomicFields = atomicFieldInit.Any(p => rootedPath.StartsWith(p));

            if (hasAtomicFields)
                DefineDefaultObjectFields(fullName, cls);
            else
                DefineDefaultDatumFields(fullName, cls);
            */
    }

    /*
    private void DefineDefaultObjectFields(string fullName, IDreamMakerSymbolResolver cls)
    {
        var baseName = DmlPath.ResolveBaseName(fullName).Replace("_", " ");
        cls.DefineFieldInitializer(DmlPath.Concat(fullName, "name"),
            SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(baseName)
            ),
            false
        );

        DefineDefaultDatumFields(fullName, cls);
    }

    private void DefineDefaultDatumFields(string fullName, IDreamMakerSymbolResolver cls)
    {
        cls.DefineFieldInitializer(
            DmlPath.Concat(fullName, "type"),
            SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(fullName)
            )
        );
    }*/
}