using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.RuntimeEnvironment;

public enum DmlPrimitiveBaseType
{
    Atom,
    Mob,
    Obj,
    Turf,
    Datum,
    Area,
    Movable,
    World,
    Client,
    Global,
    List
}

public static class RuntimeTypeResolver
{
    private static readonly Dictionary<string, DmlPrimitiveBaseType> ImmediateBaseMapping = new()
    {
        { "/atom/movable/mob", DmlPrimitiveBaseType.Mob },
        { "/atom/movable/obj", DmlPrimitiveBaseType.Obj },
        { "/atom/movable", DmlPrimitiveBaseType.Movable },
        { "/atom/turf", DmlPrimitiveBaseType.Turf },
        { "/atom/area", DmlPrimitiveBaseType.Area },
        { "/atom", DmlPrimitiveBaseType.Atom },
        { "/world", DmlPrimitiveBaseType.World },
        { "/client", DmlPrimitiveBaseType.Client },
        { "/GLOBAL", DmlPrimitiveBaseType.Global },
        { "/datum", DmlPrimitiveBaseType.Datum },
        { "/list", DmlPrimitiveBaseType.List }
    };

    private static readonly Dictionary<string, string> ClassPathExpansion = new()
    {
        { "/mob", "/atom/movable/mob" },
        { "/obj", "/atom/movable/obj" },
        { "/turf", "/atom/turf" },
        { "/area", "/atom/area" },
        { "/movable", "/atom/movable" }
    };

    public static string ExpandClassPath(string path)
    {
        foreach (var (p, r) in ClassPathExpansion)
            if (path.StartsWith(p))
                return r + path.Substring(p.Length);

        return path;
    }

    public static IEnumerable<DmlPrimitiveBaseType> EnumerateBaseTypes(string path)
    {
        var pathBuffer = ExpandClassPath(path);

        while (pathBuffer != null)
            yield return ResolveImmediateBaseType(pathBuffer, out pathBuffer);
    }

    public static bool IsMethod(Type t)
    {
        return typeof(DmlDatumProc).IsAssignableFrom(t);
    }

    public static bool InheritsBaseTypeDatum(string path, DmlPrimitiveBaseType type)
    {
        return EnumerateBaseTypes(path).Contains(type);
    }

    public static bool HasImmediateBaseTypeDatum(string path, DmlPrimitiveBaseType type)
    {
        return ResolveImmediateBaseType(path, out var _) == type;
    }

    private static DmlPrimitiveBaseType ResolveImmediateBaseType(string path, out string? super)
    {
        super = null;

        path = ExpandClassPath(path);

        string? discovered = null;

        foreach (var p in ImmediateBaseMapping.Keys)
            if (path.StartsWith(p) && (discovered == null || p.Length > discovered.Length))
                discovered = p;

        if (discovered == null)
            return DmlPrimitiveBaseType.Datum;

        super = DmlPath.ResolveParentPath(discovered);

        return ImmediateBaseMapping[discovered];
    }
}