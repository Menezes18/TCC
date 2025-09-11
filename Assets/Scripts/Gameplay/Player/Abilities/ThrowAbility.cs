using UnityEngine;

public class ThrowAbility : IPlayerAbility
{
    public string Id => "throw";

    public bool CanExecute(PlayerContext ctx)
    {
        if (!ctx.Cooldowns.IsReady(PlayerCooldownType.Throw)) return false;
        if (ctx.Player.Status != PlayerStatus.Default) return false;
        if (ctx.Player.State == PlayerState.Stagger) return false;
        if (ctx.Player.State == PlayerState.Death) return false;
        if (ctx.Player.isFrozen) return false;
        if (ctx.Player.IsCarrying) return false;
        return true;
    }

    public void Execute(PlayerContext ctx)
    {
        ctx.Player.Status = PlayerStatus.ThrowPrepare;
        // O consumo do cooldown final é feito quando realmente lança (cancel trigger atual).
    }

    public void CommitThrow(PlayerContext ctx)
    {
        ctx.Player.Status = PlayerStatus.Throw;
        ctx.Cooldowns.Start(PlayerCooldownType.Throw, ctx.Db.playerThrowCooldown);
    }
}
