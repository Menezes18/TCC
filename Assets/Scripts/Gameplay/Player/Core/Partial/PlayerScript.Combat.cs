using Mirror;
using UnityEngine;

public partial class PlayerScript : NetworkBehaviour
{
    // Combat / Action inputs (Push, Roll, Throw). No logic changes (Fase 1 structural split).
    // Fase 1 - Item 3: Guard clauses centralizadas para reduzir repetição.
    // Mantemos apenas o guard de Roll (ainda não modularizado em Ability). Push/Throw agora usam abilities.
    private bool BaseUIBlocked => panel || _chatOpen;
    private bool CanInitiateRoll()
    {
        if (isFrozen) return false;
        if (BaseUIBlocked) return false;
        if (IsAirborne) return false;
        if (State == PlayerState.Stagger) return false;
        if (!_cooldowns.IsReady(PlayerCooldownType.Roll)) return false;
        if (_isCarrying) return false;
        return true;
    }
    private void PlayerControlsSO_OnPush()
    {
        if (_pushAbility == null || _context == null) return;
        if (!_pushAbility.CanExecute(_context)) return;
        _pushAbility.Execute(_context);
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
    if (_throwAbility == null || _context == null) return;
    if (!_throwAbility.CanExecute(_context)) return;
    _throwAbility.Execute(_context);
    }

    private void PlayerControlsSO_OnThrowCancel()
    {
    // Commit do arremesso somente se estamos em preparação
    if (_throwAbility == null || _context == null) return;
    if (Status != PlayerStatus.ThrowPrepare) return; // mesma condição anterior implicitamente
    _throwAbility.CommitThrow(_context);
    }
}
