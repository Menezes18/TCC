using Mirror;
using UnityEngine;

public partial class PlayerScript : NetworkBehaviour
{
    // Combat / Action inputs (Push, Roll, Throw). No logic changes (Fase 1 structural split).
    // Fase 1 - Item 3: Guard clauses centralizadas para reduzir repetição.

    private bool BaseUIBlocked => panel || _chatOpen;
    private bool BaseStateBlocked => isFrozen || State == PlayerState.Stagger || State == PlayerState.Death;
    private bool CarryBlocked => _isCarrying;

    private bool CanInitiatePush()
    {
        if (BaseUIBlocked) return false;
        if (CarryBlocked) return false;
        if (isFrozen) return false;
        if (State == PlayerState.Stagger) return false;
        if (Status != PlayerStatus.Default || Status == PlayerStatus.Blinded) return false;
        if (!_cooldowns.IsReady(PlayerCooldownType.Push)) return false;
        return true;
    }
    private bool CanInitiateRoll()
    {
        if (isFrozen) return false;
        if (BaseUIBlocked) return false;
        if (IsAirborne) return false;
        if (State == PlayerState.Stagger) return false;
        if (!_cooldowns.IsReady(PlayerCooldownType.Roll)) return false;
        if (CarryBlocked) return false;
        return true;
    }
    private bool CanInitiateThrowPrepare()
    {
        if (isFrozen) return false;
        if (BaseUIBlocked) return false;
        if (Cursor.visible) return false;
        if (State == PlayerState.Death) return false;
        if (State == PlayerState.Stagger) return false;
        if (Status != PlayerStatus.Default) return false;
        if (!_cooldowns.IsReady(PlayerCooldownType.Throw)) return false;
        if (CarryBlocked) return false;
        return true;
    }
    private bool CanFinalizeThrow()
    {
        if (isFrozen) return false;
        if (BaseUIBlocked) return false;
        if (Cursor.visible) return false;
        if (State == PlayerState.Death) return false;
        if (State == PlayerState.Stagger) return false;
        if (Status == PlayerStatus.Pushing) return false;
        if (Status == PlayerStatus.Throw) return false;
        if (CarryBlocked) return false;
        return true;
    }
    private void PlayerControlsSO_OnPush()
    {
        if (!CanInitiatePush()) return;
        Status = PlayerStatus.Pushing;
        _cooldowns.Start(PlayerCooldownType.Push, db.playerPushCooldownTimer);
    }

    private void PlayerControlsSO_OnRoll()
    {
        if (!CanInitiateRoll()) return;

        if (_roll.magnitude == 0)
            _roll = Vector3.forward;

        State = PlayerState.Roll;
        _rollTimer = db.playerRollDuration;
    }

    private void PlayerControlsSO_OnThrow()
    {
        if (!CanInitiateThrowPrepare()) return;
        Status = PlayerStatus.ThrowPrepare;
    }

    private void PlayerControlsSO_OnThrowCancel()
    {
        if (!CanFinalizeThrow()) return;
        Status = PlayerStatus.Throw;
        Vector3 direction = _cam.forward; // mantido
        _cooldowns.Start(PlayerCooldownType.Throw, db.playerThrowCooldown);
    }
}
