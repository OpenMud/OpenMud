using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Core.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Core.Systems;

//Handles explicit requests (from outside of the runtime) to create atomics
//This is different from LogicCreationSystem (which handles requests from the Runtime, and requests related to creating
//and initializing logic.)
[WithEither(typeof(CreateAtomicComponent), typeof(CreateAtomicMobComponent))]
public class AtomicCreationSystem : AEntitySetSystem<float>
{
    private IMudEntityBuilder entityBuilder;
    private MudEnvironment environment;

    public AtomicCreationSystem(World ecsWorld, MudEnvironment environment, IMudEntityBuilder entityBuilder, bool useBuffer = false) : base(
        ecsWorld, useBuffer)
    {
        this.environment = environment;
        this.entityBuilder = entityBuilder;
    }

    private void Build(in Entity e, in CreateAtomicComponent c)
    {
        entityBuilder.CreateAtomic(e, c.ClassName, c.Identifier, c.X, c.Y, c.Z);
            
        e.Remove<CreateAtomicComponent>();
    }

    private void Build(in Entity e, in CreateAtomicMobComponent c)
    {
        var mobType = environment.World.Unwrap<GameWorld>().mob.Get<Type>();
        entityBuilder.CreateAtomic(e, environment.TypeSolver.LookupName(mobType), c.Identifier, c.X, c.Y, c.Z);
            
        e.Remove<CreateAtomicMobComponent>();
    }
    
    protected override void Update(float state, ReadOnlySpan<Entity> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.Has<CreateAtomicComponent>())
            {
                var request = entity.Get<CreateAtomicComponent>();
                Build(entity, request);
            } else if (entity.Has<CreateAtomicMobComponent>())
            {
                var request = entity.Get<CreateAtomicMobComponent>();
                Build(entity, request);
            }
        }
    }
}