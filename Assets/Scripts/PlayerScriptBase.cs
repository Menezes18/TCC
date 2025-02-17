using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerScriptBase : NetworkBehaviour
{
    private PlayerStates _state;
    public PlayerStates State{
        get => _state;
        protected set{
            if(_state == value) return;
            PlayerStates oldState = _state;
            _state = value;
            OnStateChanged(oldState, value);
        }
    }
    protected virtual void OnStateChanged(PlayerStates oldVal, PlayerStates newVal){
        Debug.LogError(oldVal + " -> " + newVal);
    }
}
