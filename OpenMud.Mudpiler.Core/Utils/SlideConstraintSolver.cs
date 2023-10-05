using System.Diagnostics;
using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

namespace OpenMud.Mudpiler.Core.Utils;

public static class SlideConstraintSolver
{
    private static bool TestEnterExitCollision(EntityHandle mover, EntityHandle[] target, EntityHandle[] origin,
        out EntityHandle? collision)
    {
        collision = null;

        var targetBlockers = target.Where(t =>
            t.HasProc("Enter") && !DmlEnv.AsLogical(t.ExecProc("Enter", mover).Result)
        );

        if (targetBlockers.Any())
        {
            collision = targetBlockers.First();
            return false;
        }

        var exitBlockers = origin.Where(t =>
            t.HasProc("Exit") && !DmlEnv.AsLogical(t.ExecProc("Exit", mover).Result)
        );

        if (exitBlockers.Any())
        {
            collision = exitBlockers.First();
            return false;
        }

        return true;
    }

    private static bool ExecuteSlideLogic(EntityHandle mover, EntityHandle[] target, EntityHandle[] origin,
        out EntityHandle? collision)
    {
        var common = target.Intersect(origin).ToList();

        var allow = TestEnterExitCollision(
            mover,
            target.Except(common).ToArray(),
            origin.Except(common).ToArray(),
            out collision
        );

        if (!allow)
        {
            Debug.Assert(collision != null);

            if (mover.HasProc("Bump"))
                mover.ExecProc("Bump", collision);
        }

        return allow;
    }

    public static void DispatchMovedLogic(LogicDirectory logicLookup, World world, Entity subject,
        SimpleDmlCoord oldLocation)
    {
        var newLocation = subject.Get<PositionComponent>();
        var curLogic = logicLookup[subject.Get<LogicIdentifierComponent>().LogicInstanceId];

        bool isAtNewLoc(in PositionComponent p)
        {
            return p.x == newLocation.x && p.y == newLocation.y;
        }

        bool isAtOldLoc(in PositionComponent p)
        {
            return p.x == oldLocation.x && p.y == oldLocation.y;
        }

        var oldLocLogics = world
            .GetEntities()
            .WithEither(typeof(TurfComponent), typeof(MobComponent), typeof(AreaComponent))
            .With<LogicIdentifierComponent>()
            .With<PositionComponent>(isAtOldLoc)
            .AsEnumerable()
            .Select(e => e.Get<LogicIdentifierComponent>().LogicInstanceId)
            .Select(logicLookup.Lookup)
            .ToArray();

        var newLocLogics = world
            .GetEntities()
            .WithEither(typeof(TurfComponent), typeof(MobComponent), typeof(AreaComponent))
            .With<LogicIdentifierComponent>()
            .With<PositionComponent>(isAtNewLoc)
            .AsEnumerable()
            .Select(e => e.Get<LogicIdentifierComponent>().LogicInstanceId)
            .Select(logicLookup.Lookup)
            .ToArray();


        var common = oldLocLogics.Intersect(newLocLogics).ToList();

        var exitedDispatch = oldLocLogics.Except(common).Where(x => x.HasProc("Exited"));
        var enteredDispatch = newLocLogics.Except(common).Where(x => x.HasProc("Entered"));

        var (x, y, z) = (oldLocation.x, oldLocation.y, oldLocation.z);

        object CreateLocArg()
        {
            return curLogic.Environment.CreateDatum("/primitive_coord",
                new WrappedProcArgumentList(oldLocation.x, oldLocation.y, oldLocation.z));
        }

        /*
            curLogic.Environment.CreateDatum(
                    DmlEnv.ClassName<VarDmlCoord>(),
                    new WrappedProcArgumentList(new[] {
                        VarEnvObjectReference.CreateImmutable(
                            new VarDmlCoord(
                                oldLocation.x,
                                oldLocation.y,
                                oldLocation.z
                            }
                        )
                    }
                )
            );*/

        foreach (var e in enteredDispatch) e.ExecProc("Entered", curLogic, CreateLocArg());

        foreach (var e in exitedDispatch) e.ExecProc("Exited", curLogic, CreateLocArg());
    }

    public static bool TestAllowSlide(LogicDirectory logicLookup, World world, Entity subject, int deltaX, int deltaY,
        out Entity? collision)
    {
        collision = null;
        if (!subject.Has<LogicIdentifierComponent>())
            return true;

        var curLogic = logicLookup[subject.Get<LogicIdentifierComponent>().LogicInstanceId];
        var oldLocation = subject.Get<PositionComponent>();
        var newLocation = (x: oldLocation.x + deltaX, y: oldLocation.y + deltaY);

        bool isAtNewLoc(in PositionComponent p)
        {
            return p.x == newLocation.x && p.y == newLocation.y;
        }

        bool isAtOldLoc(in PositionComponent p)
        {
            return p.x == oldLocation.x && p.y == oldLocation.y;
        }

        var oldLocLogics = world
            .GetEntities()
            .WithEither(typeof(TurfComponent), typeof(MobComponent), typeof(AreaComponent))
            .With<LogicIdentifierComponent>()
            .With<PositionComponent>(isAtOldLoc)
            .AsEnumerable()
            .Select(e => e.Get<LogicIdentifierComponent>().LogicInstanceId)
            .Select(logicLookup.Lookup)
            .ToArray();

        var newLocLogics = world
            .GetEntities()
            .WithEither(typeof(TurfComponent), typeof(MobComponent), typeof(AreaComponent))
            .With<LogicIdentifierComponent>()
            .With<PositionComponent>(isAtNewLoc)
            .AsEnumerable()
            .Select(e => e.Get<LogicIdentifierComponent>().LogicInstanceId)
            .Select(logicLookup.Lookup)
            .ToArray();

        var allow = ExecuteSlideLogic(curLogic, newLocLogics, oldLocLogics, out var collisionLogic);

        bool isCollider(in LogicIdentifierComponent p)
        {
            return p.LogicInstanceId == logicLookup[collisionLogic];
        }

        if (allow)
            return true;

        return false;
    }
}