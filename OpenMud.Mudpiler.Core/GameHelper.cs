using DefaultEcs;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Utils;

namespace OpenMud.Mudpiler.Core;

public interface ICommandNounSolver
{
    string? ResolveNounToTarget(string? noun);
}

public static class GameHelper
{
    public static void Slide(this IGameSimulation game, string subject, int deltaX, int deltaY)
    {
        var cost = MovementCost.Compute(deltaX, deltaY);

        bool nameMatches(in IdentifierComponent i)
        {
            return i.Name == subject;
        }

        var entity = game.World.GetEntities().With<IdentifierComponent>(nameMatches).AsEnumerable().Single();

        if (deltaX == 0 && deltaY == 0)
            entity.Remove<SlideComponent>();
        else
            entity.Set(new SlideComponent(deltaX, deltaY, cost));
    }
    
    
    public static Entity GetEntity(this IGameSimulation game, string subject)
    {
        bool nameMatches(in IdentifierComponent i)
        {
            return i.Name == subject;
        }

        var entity = game.World.GetEntities().With<IdentifierComponent>(nameMatches).AsEnumerable().SingleOrDefault();

        return entity;
    }

    public static void DispatchCommand(this IGameSimulation game, string entity, ICommandNounSolver nounSolver, string command)
    {
        var e = game.World.CreateEntity();
        
        var cmd = command.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (cmd.Length == 0)
            return;

        var verb = cmd[0];
        var noun = cmd.Skip(1).FirstOrDefault();
        var nounTarget = nounSolver.ResolveNounToTarget(noun);
        
        var operands = cmd.Skip(1).ToArray();
        
        e.Set(new ExecuteCommandComponent(entity, nounTarget, verb, operands));
    }

    public static Entity CreateMob(this IGameSimulation game, string name)
    {
        var entity = game.World.CreateEntity();

        entity.Set<CreateAtomicMobComponent>(new CreateAtomicMobComponent() { Identifier = name });

        return entity;
    }
}