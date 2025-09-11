using UnityEngine;

public class PushAbility : IPlayerAbility
{
    public string Id => "push";

    public bool CanExecute(PlayerContext ctx)
    {
        if (!ctx.Cooldowns.IsReady(PlayerCooldownType.Push)) return false;
        if (ctx.Player.Status != PlayerStatus.Default) return false;
        if (ctx.Player.State == PlayerState.Stagger) return false;
        if (ctx.Player.isFrozen) return false;
        if (ctx.Player.IsCarrying) return false;
        return true;
    }

    public void Execute(PlayerContext ctx)
    {
        ctx.Player.Status = PlayerStatus.Pushing;
        ctx.Cooldowns.Start(PlayerCooldownType.Push, ctx.Db.playerPushCooldownTimer);
    }
}
