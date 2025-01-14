using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Net.Core.Components;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Net.Core.Systems;

[With(typeof(PlayerImpersonatingComponent))]
public class GamePlayerResourceSystem : AEntitySetSystem<ServerFrame>
{
    public GamePlayerResourceSystem(World world, bool useBuffer = false) : base(world, useBuffer)
    {
        world.SubscribeComponentAdded<PlayerSessionComponent>(PlayerSessionCreated);
        world.SubscribeComponentRemoved<PlayerSessionComponent>(PlayerSessionTeardown);
    }

    private void PlayerSessionTeardown(in Entity entity, in PlayerSessionComponent value)
    {
    }

    private void PlayerSessionCreated(in Entity entity, in PlayerSessionComponent value)
    {
        var playerCharacter = World.CreateEntity();
        playerCharacter.Set(new PlayerSessionOwnedComponent(value.ConnectionId));
        playerCharacter.Set(new PlayerCanImpersonateComponent());
        playerCharacter.Set(new PlayerImpersonatingComponent(value.ConnectionId));
        playerCharacter.Set<CreateAtomicMobComponent>(new CreateAtomicMobComponent());
    }

    protected override void Update(ServerFrame state, in Entity entity)
    {
    }
}