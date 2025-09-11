using Mirror;
using UnityEngine;

public partial class PlayerScript : NetworkBehaviour
{
    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        Debug.Log($"🔁 [STATE] {oldState} → {newState}");
        if (oldState == PlayerState.Roll)
            _cooldowns.Start(PlayerCooldownType.Roll, db.playerRollCooldownDuration);
    }

    public void SetStatusDefault()
    {
        Status = PlayerStatus.Default;
    }

    // (IsCarrying já declarado no arquivo principal; duplicação removida.)
}
