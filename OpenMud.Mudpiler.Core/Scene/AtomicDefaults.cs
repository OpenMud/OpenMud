﻿using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Core.Scene;

internal class AtomicDefaults
{
    private static readonly int DEFAULT_SIGHT_RANGE = 30;

    private static readonly Dictionary<DmlPrimitive, int> TypeLayerMap = new()
    {
        { DmlPrimitive.Area, 0 },
        { DmlPrimitive.Turf, 1 },
        { DmlPrimitive.Obj, 2 },
        { DmlPrimitive.Mob, 3 }
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
            if (DmlPath.IsDeclarationInstanceOfPrimitive(typeName, l))
                return layer;

        return -1;
    }

    //Can the object look around and see in their environment
    public static bool CanSee(string typeName)
    {
        return DmlPath.IsDeclarationInstanceOfPrimitive(typeName, DmlPrimitive.Mob);
    }

    //Can the object be preceieved, seen and touched (physical)
    internal static bool IsTangible(string typeName)
    {
        return !DmlPath.IsDeclarationInstanceOfPrimitive(typeName, DmlPrimitive.Area);
    }
}