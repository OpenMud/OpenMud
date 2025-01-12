using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Core.Systems;

public class VerbRejectionException : Exception
{
    public VerbRejectionException(string reason) : base(reason)
    {
    }
}

//Handles and processes VerbCommandMessages

[With(typeof(ExecuteVerbComponent))]
public class InteractionSystem : AEntitySetSystem<float>
{
    private static readonly ArgAs[] AnythingParsePrecedence =
    {
        ArgAs.Mob,
        ArgAs.Turf,
        ArgAs.Obj,
        ArgAs.Area,
        ArgAs.Key,
        ArgAs.Icon,
        ArgAs.File,
        ArgAs.Sound,
        ArgAs.Num,
        ArgAs.Text
    };

    private readonly IEntityVisibilitySolver entityVisibilitySolver;
    private readonly LogicDirectory logicDirectory;

    private readonly Func<EnvObjectReference, object> wrap;

    public InteractionSystem(World world, IEntityVisibilitySolver entityVisibilitySolver, LogicDirectory logicDirectory,
        Func<EnvObjectReference, object> wrap, bool useBuffer = false) : base(world, useBuffer)
    {
        this.logicDirectory = logicDirectory;
        this.wrap = wrap;
        this.entityVisibilitySolver = entityVisibilitySolver;
    }

    private void ValidateSourceType(SimpleSourceConstraint details, EnvObjectReference logicInstance, ref Entity user, string usrName)
    {
        if (!typeof(Atom).IsAssignableFrom(logicInstance.Type))
            throw new VerbRejectionException("Source type is not an atomic. Only atomics supported currently.");

        var entityHandle = wrap(logicInstance) as EntityHandle;

        if (entityHandle == null)
            throw new VerbRejectionException("Invalid source type, must be an atomic.");

        var instanceId = logicDirectory[entityHandle];

        bool MatchLogic(in LogicIdentifierComponent c)
        {
            return c.LogicInstanceId == instanceId;
        }

        var entity = World.GetEntities().With<LogicIdentifierComponent>(MatchLogic).With<IdentifierComponent>()
            .AsEnumerable().FirstOrDefault();

        if (!entity.IsAlive)
            throw new VerbRejectionException("Source does not have a name.");

        VerbConstraintSolver.ValidateSourceType(
            World,
            entityVisibilitySolver,
            new VerbSrc(details.Src, details.SrcOperand),
            ref entity,
            ref user
        );
    }

    private void ValidateVerbSource(ref Entity target, VerbDetails details, string source, string destination)
    {
        bool TestName(in IdentifierComponent i)
        {
            return i.Name == source;
        }

        var sourceEntity = World.GetEntities().With<IdentifierComponent>(TestName).AsEnumerable().SingleOrDefault();

        VerbConstraintSolver.ValidateSourceType(World, entityVisibilitySolver, details.SrcConstraints, ref target,
            ref sourceEntity);
    }


    private EnvObjectReference[] ParseAndValidateVerbArguments(ref Entity target, VerbDetails details, string source,
        string[] arguments)
    {
        List<EnvObjectReference> compiledArguments = new();

        for (var i = 0; i < arguments.Length; i++)
        {
            var argAsConstraints = details.ArgAsConstraints.Where(x => x.ArgIndex == i).ToList();
            var argSrcConstraints = details.ArgSourceConstraints.Where(x => x.ArgIndex == i).ToList();
            var listEvalConstraints = details.ListEvalArgSourceConstraints.Where(x => x.ArgIndex == i).ToList();

            var argument = ParseVerbArg(argAsConstraints, arguments[i]);

            ValidateArgument(argSrcConstraints, listEvalConstraints, target, source, argument);

            compiledArguments.Add(argument);
        }

        return compiledArguments.ToArray();
    }

    private void ValidateArgument(List<SimpleSourceConstraint> argSrcConstraints,
        List<ListEvalSourceConstraint> listEvalConstraints, Entity usr, string usrName, EnvObjectReference? argument)
    {
        foreach (var c in argSrcConstraints)
            ValidateSourceType(c, argument, ref usr, usrName);

        foreach (var c in listEvalConstraints)
        {
            if (!usr.Has<LogicIdentifierComponent>())
                throw new VerbRejectionException(
                    "Cannot evaluate the list constraint on an uninitialized target logic.");

            var logic = logicDirectory[usr.Get<LogicIdentifierComponent>().LogicInstanceId];
            var collection = c.generators.Select(g => logic.ExecProc(g).Result).ToList();

            if (!collection.Contains(argument))
                throw new VerbRejectionException("Argument not in allowed source list.");
        }
    }

    private bool TryParseArgAs(ArgAs constraint, string argument, out EnvObjectReference parsed)
    {
        parsed = VarEnvObjectReference.NULL;

        bool EntityQualifies(in IdentifierComponent n)
        {
            return n.Name == argument;
        }

        bool ParseLogicDatum<T>(out EnvObjectReference p)
        {
            p = VarEnvObjectReference.NULL;
            var e = World.GetEntities().With<IdentifierComponent>(EntityQualifies).With<LogicIdentifierComponent>()
                .With<T>().AsEnumerable().FirstOrDefault();

            if (!e.IsAlive || !e.Has<LogicIdentifierComponent>())
                return false;

            var r = logicDirectory[e.Get<LogicIdentifierComponent>().LogicInstanceId].Unwrap<object>();
            p = VarEnvObjectReference.CreateImmutable(r);
            return true;
        }

        switch (constraint)
        {
            case ArgAs.Mob:
                throw new NotImplementedException(); //return ParseLogicDatum<MovableComponent>(out parsed);
            case ArgAs.Obj:
                throw new NotImplementedException(); // return ParseLogicDatum<ObjComponent>(out parsed);
            case ArgAs.Turf:
                return ParseLogicDatum<TurfComponent>(out parsed);
            case ArgAs.Area:
                return ParseLogicDatum<AreaComponent>(out parsed);
            case ArgAs.Num:
                int parsedBuffer;
                if (!int.TryParse(argument, out parsedBuffer))
                    return false;

                parsed = VarEnvObjectReference.CreateImmutable(parsedBuffer);
                return true;
            case ArgAs.Text:
                parsed = VarEnvObjectReference.CreateImmutable(argument);
                return true;
            case ArgAs.Password:
                parsed = VarEnvObjectReference.CreateImmutable(argument);
                return true;
            case ArgAs.Null:
                if (argument == "")
                    return true;

                return false;
            case ArgAs.Message:
            case ArgAs.CommandText:
            case ArgAs.Icon:
            case ArgAs.Key:
            case ArgAs.Color:
            case ArgAs.File:
            case ArgAs.Sound:
                throw new NotImplementedException();
            default:
                throw new Exception("Unknown argas constraint.");
        }
    }

    private EnvObjectReference ParseVerbArg(List<ArgAsConstraint> argAsConstraints, string argument)
    {
        var parsed = VarEnvObjectReference.NULL;

        var argAsParseOrder = argAsConstraints.Select(m => m.Expected).ToList();

        if (argAsParseOrder.Count == 0)
            argAsParseOrder.Add(ArgAs.Text);

        var anythingIdx = argAsParseOrder.IndexOf(ArgAs.Anything);

        if (anythingIdx >= 0)
        {
            argAsParseOrder.InsertRange(anythingIdx, AnythingParsePrecedence.Except(argAsParseOrder).ToList());
            argAsParseOrder.RemoveAll(x => x == ArgAs.Anything);
        }

        var parserMatched = false;

        foreach (var c in argAsConstraints)
        {
            parserMatched = TryParseArgAs(c.Expected, argument, out parsed);
            if (parserMatched)
                break;
        }

        if (!parserMatched)
            throw new VerbRejectionException("Argument did not satisfy any of the arg as constraints.");

        return parsed;
    }

    private void ProcessVerb(in Entity verbEntity, ref Entity targetEntity, VerbDetails details,
        ExecuteVerbComponent execute)
    {
        ValidateVerbSource(ref targetEntity, details, execute.SourceName, execute.DestinationName);
        object[] arguments =
            ParseAndValidateVerbArguments(ref targetEntity, details, execute.SourceName, execute.Arguments);

        verbEntity.Remove<ExecuteVerbComponent>();
        verbEntity.Set(new ExecuteLogicComponent(execute.SourceName, execute.DestinationName, details.MethodName,
            arguments));
    }

    protected override void Update(float state, in Entity entity)
    {
        var exec = entity.Get<ExecuteVerbComponent>();

        bool MatchDestinationName(in IdentifierComponent n)
        {
            return exec.DestinationName == n.Name;
        }

        var destEntity = World.GetEntities().With<IdentifierComponent>(MatchDestinationName).AsEnumerable()
            .FirstOrDefault();

        if (!destEntity.Has<VerbDetailsComponent>())
            return;

        var verbDetails = destEntity.Get<VerbDetailsComponent>();

        try
        {
            ProcessVerb(in entity, ref destEntity, verbDetails.Verbs[exec.Verb], exec);
        }
        catch (VerbRejectionException e)
        {
            entity.Dispose();
            World.Publish(new VerbRejectionMessage { Source = exec.SourceName, Reason = e.Message });
        }

        base.Update(state, entity);
    }
}