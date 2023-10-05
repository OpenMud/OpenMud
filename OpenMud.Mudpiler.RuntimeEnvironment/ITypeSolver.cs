namespace OpenMud.Mudpiler.RuntimeEnvironment;

public interface ITypeSolver
{
    IEnumerable<Type> Types { get; }

    bool IsTypeKnown(string typeName);
    Type Lookup(string name, int maxDeclarationOrder = int.MaxValue);

    string LookupName(Type t);

    Type[] SubClasses(Type s);
}