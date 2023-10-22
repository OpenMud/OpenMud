using System.Reflection;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Utils;

public static class DmlEnv
{
    public static string ClassName(Type t)
    {
        return t.GetCustomAttributes(typeof(EntityDefinition)).Cast<EntityDefinition>().First().Name;
    }

    public static string ClassName<T>()
    {
        return ClassName(typeof(T));
    }

    public static string AsClassName(object t)
    {
        if (t is string s)
            return s;
        if (t is Type typ)
            return ClassName(typ);

        throw new Exception("Unknown class type.");
    }

    public static string? AsText(object? v)
    {
        if (v is EnvObjectReference w)
        {
            if (w.IsNull)
                return null;

            return AsText(w.Target);
        }

        if(v == null)
            return null;

        if (v is string s)
            return s;

        return v.ToString();
    }

    public static bool IsNumericType(object? o)
    {
        return o != null && IsNumericType(o.GetType());
    }

    public static bool IsNumericType(Type t)
    {
        switch (Type.GetTypeCode(t))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    public static int AsNumeric(EnvObjectReference? subject)
    {
        if (subject == null || subject.IsNull)
            return 0;

        return AsNumeric(subject.Get<object>());
    }

    public static float AsDecimal(EnvObjectReference? subject)
    {
        if (subject == null || subject.IsNull)
            return 0;

        return AsDecimal(subject.Get<object>());
    }

    public static int AsNumeric(object? subject)
    {
        if (subject == null)
            return 0;
        if (typeof(bool).IsEquivalentTo(subject.GetType()))
            return (bool)subject ? 1 : 0;
        if (IsNumericType(subject))
            return (int)subject;
        throw new Exception("Unable to interpret as numeric.");
    }

    public static float AsDecimal(object? subject)
    {
        if (subject == null)
            return 0;
        if (typeof(bool).IsEquivalentTo(subject.GetType()))
            return (bool)subject ? 1 : 0;
        if (IsNumericType(subject))
            return (float)Convert.ChangeType(subject, typeof(float));
        throw new Exception("Unable to interpret as numeric.");
    }

    public static bool AsLogical(EnvObjectReference? subject)
    {
        if (subject == null || subject.IsNull)
            return false;

        return AsLogical(subject.Target);
    }

    public static bool AsLogical(object? subject)
    {
        if (subject == null)
            return false;
        if (typeof(bool).IsEquivalentTo(subject.GetType()))
            return (bool)subject;
        if (IsNumericType(subject) && (double)Convert.ChangeType(subject, typeof(double)) == 0)
            return false;
        return true;
    }

    public static SimpleDmlCoord? ParseCoord(DatumHandle r)
    {
        return ParseCoord(
            VarEnvObjectReference.CreateImmutable(
                new SimpleDmlCoord(
                    (int)r["x"],
                    (int)r["y"],
                    (int)r["layer"]
                )
            )
        );
    }


    public static SimpleDmlCoord? ParseCoord(Atom r)
    {
        return ParseCoord(
            VarEnvObjectReference.CreateImmutable(
                new SimpleDmlCoord(
                    (int)r.x,
                    (int)r.y,
                    (int)r.layer
                )
            )
        );
    }

    public static SimpleDmlCoord? ParseCoord(EnvObjectReference r)
    {
        if (typeof(DmlCoord).IsAssignableFrom(r.Type))
        {
            var coordBuffer = r.Get<DmlCoord>();
            return new SimpleDmlCoord(coordBuffer.x, coordBuffer.y, coordBuffer.z);
        }

        if (typeof(SimpleDmlCoord).IsAssignableFrom(r.Type))
        {
            var coordBuffer = r.Get<SimpleDmlCoord>();
            return new SimpleDmlCoord(coordBuffer.x, coordBuffer.y, coordBuffer.z);
        }

        if (typeof(Atom).IsAssignableFrom(r.Type))
        {
            var coordBuffer = r.Get<Atom>();
            return new SimpleDmlCoord(coordBuffer.x, coordBuffer.y, coordBuffer.layer);
        }

        if (!AsLogical(r.Target)) return null;

        throw new Exception("Not a valid coord.");
    }

    public static int AsDirection(int deltaX, int deltaY)
    {
        if (deltaX > 0)
        {
            if (deltaY == 0)
                return EnvironmentConstants.EAST;
            if (deltaY > 0)
                return EnvironmentConstants.SOUTHEAST;
            return EnvironmentConstants.NORTHEAST;
        }

        if (deltaX < 0)
        {
            if (deltaY == 0)
                return EnvironmentConstants.WEST;
            if (deltaY > 0)
                return EnvironmentConstants.SOUTHWEST;
            return EnvironmentConstants.NORTHWEST;
        }

        if (deltaX == 0)
        {
            if (deltaY > 0)
                return EnvironmentConstants.SOUTH;
            return EnvironmentConstants.NORTH;
        }

        return EnvironmentConstants.SOUTH;
    }
}