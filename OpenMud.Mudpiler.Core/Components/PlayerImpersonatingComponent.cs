namespace OpenMud.Mudpiler.Core.Components;

public class PlayerImpersonatingComponent
{
    public readonly string? PlayerId;

    public PlayerImpersonatingComponent(string playerId)
    {
        PlayerId = playerId;
    }
}