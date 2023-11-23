using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;
using System.IO;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class SimpleTypeSolver : ITypeSolver
{
    private readonly Dictionary<Tuple<int, string>, Type> typeLibrary;

    public SimpleTypeSolver(Dictionary<Tuple<int, string>, Type> types)
    {
        typeLibrary = types.ToDictionary(x => x.Key, x => x.Value);
    }

    public IEnumerable<Type> Types => typeLibrary.Values.ToList();

    public Type Lookup(string name, int maxDeclarationOrder = int.MaxValue)
    {
        var r = LookupOrDefault(name, maxDeclarationOrder);

        if (r == null)
            throw new Exception("Type name could not be resolved: " + name);

        return r;
    }

    public Type? LookupOrDefault(string name, int maxDeclarationOrder = int.MaxValue, Type? defaultType = null)
    {
        if (name.Length == 0 || name == "/")
            return typeof(object);

        var fullyQualifiedTypeName = DmlPath.BuildQualifiedDeclarationName(name);

        var t = typeLibrary.Where(x =>
                x.Key.Item1 <= maxDeclarationOrder && x.Key.Item2 == fullyQualifiedTypeName
            )
            .OrderByDescending(x => x.Key)
            .Select(x => x.Value).FirstOrDefault();

        return t ?? defaultType;
    }

    public string LookupName(Type t)
    {
        return typeLibrary.Where(x => x.Value.IsEquivalentTo(t)).Select(x => x.Key.Item2).First();
    }

    public bool IsTypeKnown(string typeName)
    {
        return typeLibrary.Keys.Any(k => k.Item2 == DmlPath.RootClassName(typeName));
    }

    public Type[] SubClasses(Type s)
    {
        var subClasses = typeLibrary.Values.Distinct().Where(
            k => {
                if (k == s) return false;

                return s.IsAssignableFrom(k);
            }
        );

        return subClasses.ToArray();
    }

    public Tuple<int, string> Lookup(Type t)
    {
        return typeLibrary.Where(x => x.Value.IsEquivalentTo(t)).Select(x => x.Key).First();
    }

    public bool InheritsPrimitive(string path, DmlPrimitive type)
    {
        var t = LookupOrDefault(path);

        if (t == null)
            throw new Exception("Unknown type.");

        //Procedures are an exception. Their type paths do no represent their inheritance hierarchy, but instead their location.
        //For example, the method xyz on object wuv would have the type path /wuv/xyz, but in this case the method (xyz) does not inherit from
        //the base class wub.
        if (typeof(DatumProc).IsAssignableFrom(t))
            return false;

        return DmlPath.EnumerateBaseTypes(path).Contains(type);
    }

    public bool IsMethod(Type t)
    {
        return typeof(DmlDatumProc).IsAssignableFrom(t);
    }
}