using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Core.Scene;

internal class AtomicDefaults
{
    private static readonly int DEFAULT_SIGHT_RANGE = 30;

    private static readonly Dictionary<DmlPrimitiveBaseType, int> TypeLayerMap = new()
    {
        { DmlPrimitiveBaseType.Area, 0 },
        { DmlPrimitiveBaseType.Turf, 1 },
        { DmlPrimitiveBaseType.Obj, 2 },
        { DmlPrimitiveBaseType.Mob, 3 }
    };

    public static int SightRange(string className)
    {
        if (!CanSee(className))
            return 0;

        return DEFAULT_SIGHT_RANGE;
    }

    public static int IdentifyDefaultLayer(string typeName)
    {
        foreach (var (l, layer) in TypeLayerMap)
            if (RuntimeTypeResolver.InheritsBaseTypeDatum(typeName, l))
                return layer;

        return -1;
    }

    //Can the object look around and see in their environment
    public static bool CanSee(string typeName)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(typeName, DmlPrimitiveBaseType.Mob);
    }

    //Can the object be preceieved, seen and touched (physical)
    internal static bool IsTangible(string typeName)
    {
        return !RuntimeTypeResolver.InheritsBaseTypeDatum(typeName, DmlPrimitiveBaseType.Area);
    }
}