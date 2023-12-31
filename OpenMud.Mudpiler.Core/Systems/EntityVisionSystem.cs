﻿using System.Collections.Immutable;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(VisionComponent))]
public class EntityVisionSystem : AEntitySetSystem<float>
{
    private readonly EntityVisibilitySolver visibilitySolver;

    public EntityVisionSystem(World world, EntityVisibilitySolver visibilitySolver, bool useBuffer = false) : base(
        world, useBuffer)
    {
        this.visibilitySolver = visibilitySolver;
    }

    protected override void Update(float state, in Entity entity)
    {
        var visible = visibilitySolver.ComputeVisible(World, entity, entity.Get<VisionComponent>().Range);

        var newComponent = new EntityVisibilityComponent(visible.Keys.ToImmutableHashSet());

        if (entity.Has<EntityVisibilityComponent>())
            if (newComponent.Equals(entity.Get<EntityVisibilityComponent>()))
                return;

        entity.Set(newComponent);
    }
}