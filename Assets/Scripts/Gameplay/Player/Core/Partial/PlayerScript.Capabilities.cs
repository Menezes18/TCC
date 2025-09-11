using UnityEngine;

// Fase 4 - Item 12: Implementação das capacidades segregadas.
public partial class PlayerScript : IPushCapability, IThrowCapability, IRollCapability, IJumpCapability
{
    // PUSH
    public void ExecutePush(PlayerContext ctx)
    {
        Status = PlayerStatus.Pushing;
        ctx.Cooldowns.Start(PlayerCooldownType.Push, ctx.Db.playerPushCooldownTimer);
    }

    // THROW
    public void BeginThrow(PlayerContext ctx)
    {
        Status = PlayerStatus.ThrowPrepare;
    }

    public void CommitThrow(PlayerContext ctx)
    {
        Status = PlayerStatus.Throw;
        ctx.Cooldowns.Start(PlayerCooldownType.Throw, ctx.Db.playerThrowCooldown);
    }

    // ROLL
    public bool CanRoll(PlayerContext ctx)
    {
        if (isFrozen) return false;
        if (panel || _chatOpen) return false;
        if (IsAirborne) return false;
        if (State == PlayerState.Stagger) return false;
        if (!_cooldowns.IsReady(PlayerCooldownType.Roll)) return false;
        if (_isCarrying) return false;
        return true;
    }

    public void ExecuteRoll(PlayerContext ctx)
    {
        if (!CanRoll(ctx)) return;
        if (_roll.magnitude == 0)
            _roll = Vector3.forward;
        State = PlayerState.Roll;
        _rollTimer = db.playerRollDuration;
    }

    // JUMP
    public bool CanJump(PlayerContext ctx)
    {
        if (panel) return false;
        if (_chatOpen) return false;
        if (isFrozen) return false;
        if (_isCarrying) return false;
        if (State != PlayerState.Default) return false;
        return true;
    }

    public void ExecuteJump(PlayerContext ctx)
    {
        if (!CanJump(ctx)) return;
        State = PlayerState.Ascend;
        _ignoreGroundedNextFrame = true;
        _move.y = db.playerJumpHeight;
        _inertia = new Vector3(_move.x, 0, _move.z);
        InertiaCap = _inertia.magnitude;
    }
}
