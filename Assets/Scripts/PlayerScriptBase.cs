using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System;
public class PlayerScriptBase : NetworkBehaviour
{
    private PlayerStates _state;
    
    
    public event Action<PlayerStates, PlayerStates> OnStateChangeEvent;
    
    
    public PlayerStates State
    {
        get => _state;
           set{
            if(_state == value) return;
            PlayerStates oldState = _state;
            _state = value;
            OnStateChanged(oldState, value);
            OnStateChangeEvent?.Invoke(oldState, value);
        }
    }
    
    // Verificação se o estado atual é um dos estados especificados
    public bool IsInState(params PlayerStates[] states)
    {
        foreach (var state in states)
        {
            if (_state == state)
                return true;
        }
        return false;
    }
    protected virtual void OnStateChanged(PlayerStates oldVal, PlayerStates newVal)
    {
        Debug.LogError($"[{gameObject.name}] Estado alterado: {oldVal} -> {newVal}");
    }
}