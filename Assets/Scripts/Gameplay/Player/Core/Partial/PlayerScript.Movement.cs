using Mirror;
using UnityEngine;

public partial class PlayerScript : NetworkBehaviour
{
    // Movement & Aerial related methods moved from original monolithic file (Fase 1 - split estrutural).
    private void AerialDetection()
    {
        if (State == PlayerState.Death) return;
        if (State == PlayerState.Stagger) return;
        if (State == PlayerState.Roll) return;

        if (_move.y > 0)
            State = PlayerState.Ascend;
        else if (_move.y < db.gravityGrounded)
            State = PlayerState.Descend;

        if (_ignoreGroundedNextFrame == true) {
            _ignoreGroundedNextFrame = false;
            return;
        }

        if (_controller.isGrounded == true) {
            State = PlayerState.Default;
        }
    }

    private void AerialBehaviour()
    {
        if (State != PlayerState.Ascend && State != PlayerState.Descend) return;

        float vertical = _move.y;

        Vector3 input = new Vector3(_input.x, 0, _input.z);
        input = Quaternion.Euler(rot) * input;
        input *= (db.playerAirSpeed * GetSpeedMultiplier()) * Time.deltaTime;
        _inertia += input;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;
    }

    private void DefaultBehaviour()
    {
        if (State != PlayerState.Default) return;
        float vertical = _move.y;

        _move = _input;
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed * GetSpeedMultiplier();

        _move.y = vertical;
        _move += Vector3.up * db.gravity;
    }

    public void SetDefaultState()
    {
        if (!_controller.isGrounded) {
            if (_move.y > 0)
                State = PlayerState.Ascend;
            else
                State = PlayerState.Descend;
        }
        State = PlayerState.Default;
    }

    public PlayerState GetDefaultStatus()
    {
        if (_move.y > 0)
            return PlayerState.Ascend;
        if (_move.y < -1)
            return PlayerState.Descend;

        return PlayerState.Default;
    }
}
