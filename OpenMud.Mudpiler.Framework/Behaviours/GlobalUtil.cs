using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class GlobalUtil : IRuntimeTypeBuilder
{
    private IDmlTaskScheduler scheduler;

    public GlobalUtil(IDmlTaskScheduler scheduler)
    {
        this.scheduler = scheduler;
    }

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
        procedureCollection.Register(0, new ActionDatumProc(RuntimeFrameworkIntrinsic.INDIRECT_CALL, indirect_call));
        
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

    private EnvObjectReference CallHelper(DatumProcExecutionContext ctx)
    {
        try
        {
            ctx.Continue();
        }
        catch (DeferExecutionException e)
        {
            this.scheduler.DeferExecution(ctx, e.LengthMilliseconds);
        }

        return ctx.Result;
    }

    public EnvObjectReference indirect_call_external_library(string libName, string procName, ProcArgumentList args, Datum self)
    {
        throw new Exception("Interfacing with external libraries is not supported");
    }

    public EnvObjectReference indirect_call_instance(Datum instance, string procName, ProcArgumentList args, Datum self)
    {
        return CallHelper(instance.Invoke(null, procName, null, args));
    }

    public EnvObjectReference indirect_call_static(Type typeName, ProcArgumentList args, Datum self)
    {
        if (!typeName.IsAssignableTo(typeof(DatumProc)))
            throw new Exception("Not a valid method name.");

        var proc = (DatumProc)Activator.CreateInstance(typeName);

        var ctx = proc.Create();

        ctx.SetupContext(null, null, self, self.ctx);
        ctx.ActiveArguments = proc.DefaultArgumentList().Overlay(args);

        return CallHelper(ctx);
    }


    public EnvObjectReference indirect_call(ProcArgumentList args, Datum self)
    {
        //First two arguments are allocated for the indirect_call, and the remainder the target.
        var (callArgs, targetArgs) = args.Split(2);

        if (callArgs.MaxPositionalArgument == 2)
        {
            if (callArgs[0].Type == typeof(string) && callArgs[1].Type == typeof(string))
                return indirect_call_external_library(callArgs[0].Get<string>(), callArgs[1].Get<string>(), targetArgs, self);
            
            if (callArgs[0].Type.IsAssignableTo(typeof(Datum)) && callArgs[1].Type == typeof(string))
                return indirect_call_instance(callArgs[0].Get<Datum>(), callArgs[1].Get<string>(), targetArgs, self);

        } 
        
        if (callArgs.MaxPositionalArgument >= 1 && typeof(Type).IsAssignableFrom(callArgs[0].Type))
            return indirect_call_static(callArgs[0].Get<Type>(), targetArgs, self);
        
        throw new Exception("Invalid arguments for an indirect call.");
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