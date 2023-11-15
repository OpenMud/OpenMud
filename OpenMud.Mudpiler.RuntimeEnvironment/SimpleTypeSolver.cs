using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

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
        var normalName = DmlPath.NormalizeTypeName(name);

        if (normalName == "/")
            return typeof(object);

        var t = typeLibrary.Where(x =>
                x.Key.Item1 <= maxDeclarationOrder && x.Key.Item2 == normalName
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
        var subject = Lookup(s);

        var subClasses = typeLibrary.Keys.Where(
            k => k != subject && k.Item2.StartsWith(subject.Item2));

        return subClasses.Select(k => typeLibrary[k]).ToArray();
    }

    public Tuple<int, string> Lookup(Type t)
    {
        return typeLibrary.Where(x => x.Value.IsEquivalentTo(t)).Select(x => x.Key).First();
    }
}