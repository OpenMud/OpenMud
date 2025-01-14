using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Systems;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;

namespace OpenMud.Mudpiler.Core.Utils;

public static class VerbConstraintSolver
{
    public static bool TestSourceType(World world, IEntityVisibilitySolver visibilitySolver, VerbSrc? definedSource,
        ref Entity target, ref Entity source, out string reason)
    {
        var targetName = target.Get<IdentifierComponent>().Name;
        var usrName = source.Get<IdentifierComponent>().Name;

        var details = definedSource ?? GetDefaultVerbConstraints(ref target);

        reason = null;

        switch (details.Source)
        {
            case SourceType.User:
                if (!targetName.Equals(usrName))
                {
                    reason = "The user of this verb must be the source.";
                    return false;
                }

                break;
            case SourceType.UserContents:
                if (!source.Has<AtomicContentsComponent>() ||
                    !source.Get<AtomicContentsComponent>().Contents.Contains(targetName))
                {
                    reason = "The source does not contain the target entity.";
                    return false;
                }

                break;
            case SourceType.UserLoc:
                throw new NotImplementedException();
                break;
            case SourceType.UserGroup:
                throw new NotImplementedException();
                break;
            case SourceType.View:
            {
                if (!visibilitySolver.ComputeVisible(world, source, details.Argument)
                        .TryGetValue(targetName, out var distance))
                {
                    reason = "The source is not visible to the subject.";
                    return false;
                }

                if (distance > details.Argument)
                {
                    reason = "View distance is too far to use this verb.";
                    return false;
                }

                break;
            }
            case SourceType.OView:
            {
                if (source == target)
                {
                    reason = "The user cannot be the subject itself for an oview constraint";
                    return false;
                }

                if (source.Get<AtomicContentsComponent>().Contents.Contains(targetName))
                {
                    reason = "The user cannot be contained by the subject for an oview constraint";
                    return false;
                }

                var src = new VerbSrc(SourceType.View, definedSource.Argument);

                return TestSourceType(world, visibilitySolver, src, ref target, ref source, out reason);
            }
            case SourceType.Any:
                break;
            default:
                throw new Exception("Unknown source type constraint.");
        }

        return true;
    }


    public static void ValidateSourceType(World world, IEntityVisibilitySolver visiblitySolver, VerbSrc? details,
        ref Entity target, ref Entity source)
    {
        if (!TestSourceType(world, visiblitySolver, details, ref target, ref source, out var reason))
            throw new VerbRejectionException(reason);
    }

    private static bool IsVerbUsable(World world, IEntityVisibilitySolver visiblitySolver, ref Entity target,
        ref Entity source, VerbDetails details)
    {
        if (!target.Has<IdentifierComponent>())
            return false;

        return TestSourceType(world, visiblitySolver, details.SrcConstraints, ref target, ref source, out var _);
    }


    public static CommandDetails[] DiscoverSelfCommands(World world, IEntityVisibilitySolver visiblitySolver,
        Entity host, int precedent)
    {
        if (!host.Has<VerbDetailsComponent>() || !host.Has<IdentifierComponent>())
            return new CommandDetails[0];

        var verbs = host.Get<VerbDetailsComponent>();

        var commands = new List<CommandDetails>();

        foreach (var v in verbs.Verbs)
            if (IsVerbUsable(world, visiblitySolver, ref host, ref host, v.Value))
                commands.Add(new CommandDetails(precedent, v.Key));

        return commands.ToArray();
    }

    public static VerbSrc GetDefaultVerbConstraints(ref Entity target)
    {
        if (target.Has<TurfComponent>())
            return new VerbSrc(SourceType.View, 0);

        if (target.Has<AreaComponent>())
            return new VerbSrc(SourceType.View, 0);

        if (target.Has<MobComponent>())
            return new VerbSrc(SourceType.User);

        if (target.Has<ObjComponent>())
            return new VerbSrc(SourceType.UserContents);

        throw new Exception("Unidentified verb host.");
    }

    public static CommandDetails[] DiscoverExternalInteractionCommands(World world,
        IEntityVisibilitySolver visiblitySolver, Entity host, Entity target, int precedent)
    {
        if (!target.Has<VerbDetailsComponent>() || !target.Has<IdentifierComponent>())
            return new CommandDetails[0];

        var otherId = target.Get<IdentifierComponent>().Name;
        var otherName = target.Has<DisplayNameComponent>() ? target.Get<DisplayNameComponent>().Name : null;
        var verbs = target.Get<VerbDetailsComponent>();

        var commands = new List<CommandDetails>();

        foreach (var v in verbs.Verbs)
            if (IsVerbUsable(world, visiblitySolver, ref target, ref host, v.Value))
                commands.Add(new CommandDetails(precedent, v.Key, otherName, otherId));

        return commands.ToArray();
    }
}