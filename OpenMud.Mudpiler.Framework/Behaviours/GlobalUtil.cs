using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class GlobalUtil : IRuntimeTypeBuilder
{
    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0, new ActionDatumProc("turn", args => turn(args[0], args[1])));
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.HasImmediateBaseTypeDatum(target, DmlPrimitiveBaseType.Global);
    }

    public EnvObjectReference turn(EnvObjectReference envDir, EnvObjectReference envAngle)
    {
        var orderedDir = new[]
        {
            EnvironmentConstants.NORTH,
            EnvironmentConstants.NORTHWEST,
            EnvironmentConstants.WEST,
            EnvironmentConstants.SOUTHWEST,
            EnvironmentConstants.SOUTH,
            EnvironmentConstants.SOUTHEAST,
            EnvironmentConstants.EAST,
            EnvironmentConstants.NORTHEAST
        };

        var angle = envAngle.GetOrDefault(0);
        var dirCode = envDir.GetOrDefault(0);

        if (angle == 0)
            return dirCode;

        var curIdx = Array.IndexOf(orderedDir, dirCode);

        if (curIdx < 0)
            return orderedDir[new Random().Next(0, orderedDir.Length)];

        var closestIntervalAngle = (int)(Math.Round(Math.Abs(angle) / 45.0) * 45) * Math.Sign(angle);

        curIdx += closestIntervalAngle / 45;

        if (curIdx >= 0)
            return orderedDir[curIdx % orderedDir.Length];

        var wrapCount = Math.Abs(curIdx) / orderedDir.Length;

        return orderedDir[orderedDir.Length - (Math.Abs(curIdx) - wrapCount * orderedDir.Length)];
    }
}