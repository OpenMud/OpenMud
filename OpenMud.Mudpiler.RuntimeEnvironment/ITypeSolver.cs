using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public interface ITypeSolver
{
    IEnumerable<Type> Types { get; }

    bool IsTypeKnown(string typeName);
    Type Lookup(string name, int maxDeclarationOrder = int.MaxValue);
    Type? LookupOrDefault(string name, int maxDeclarationOrder = int.MaxValue, Type? defaultType = null);

    string LookupName(Type t);

    Type[] SubClasses(Type s);

    bool InheritsPrimitive(string v, DmlPrimitive turf);

    bool IsMethod(Type t);
}