using System.Text;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class GlobalUtil : IRuntimeTypeBuilder
{
    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0, new ActionDatumProc("turn", args => turn(args[0], args[1])));
        procedureCollection.Register(0, new ActionDatumProc("findtext",
            args => findtext(
                args[0, "Haystack"],
                args[1, "Needle"],
                args[2, "Start"],
                args[3, "End"]
            )));

        procedureCollection.Register(0, new ActionDatumProc(RuntimeFrameworkIntrinsic.TEXT, text));
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.HasImmediateBaseTypeDatum(target, DmlPrimitiveBaseType.Global);
    }

    public EnvObjectReference findtext(EnvObjectReference haystack, EnvObjectReference needle, EnvObjectReference start,
        EnvObjectReference end)
    {
        var startIdx = DmlEnv.AsNumeric(start.GetOrDefault(1)) - 1;
        var endIdx = DmlEnv.AsNumeric(end.GetOrDefault(0)) - 1;

        var haystackText = (DmlEnv.AsText(haystack.GetOrDefault("")) ?? "").ToLower();
        var needleText = (DmlEnv.AsText(needle.GetOrDefault("")) ?? "").ToLower();

        if (needleText.Length == 0 || haystackText.Length == 0)
            return VarEnvObjectReference.CreateImmutable(0);

        var searchCount = endIdx < 0 ? -1 : endIdx - startIdx;

        var pos = searchCount < 0 ?
            haystackText.IndexOf(needleText, startIdx) :
            haystackText.IndexOf(needleText, startIdx, searchCount);

        return VarEnvObjectReference.CreateImmutable(pos + 1);
    }

    public EnvObjectReference text(ProcArgumentList args)
    {
        var formatString = DmlEnv.AsText(args.Get(0).GetOrDefault("")) ?? "";

        var formatArgs = args.GetArgumentList().Skip(1).Select(DmlEnv.AsText).Select(x => x ?? "").ToArray();

        var r = new StringBuilder();

        bool escaped = false;
        var subIdx = 0;

        for (var i = 0; i < formatString.Length; i++)
        {
            var c = formatString[i];
            char? next = i + 1 >= formatString.Length ? null : formatString[i + 1];

            if (escaped)
            {
                r.Append(c);
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
                continue;
            } else if (c == '[' && next == ']')
            {
                var subStr = subIdx >= formatArgs.Length ? "" : formatArgs[subIdx++];
                r.Append(subStr);
                i++;
            }
            else
                r.Append(c);
        }

        return VarEnvObjectReference.CreateImmutable(r.ToString());
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