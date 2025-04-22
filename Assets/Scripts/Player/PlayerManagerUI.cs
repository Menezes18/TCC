using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{

    public PlayerControlSO playerControlSO;
    private PlayerScript _playerScript;
    private bool _valueCelular = false;
    
    public GameObject celular;
    private void Start()
    {
        if (!isLocalPlayer) return;  
        celular.SetActive(false);
        _playerScript = GetComponent<PlayerScript>();
        playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
    }

    private void EventOnCelularMenu(InputAction.CallbackContext obj)
    {
        bool newValue = !_valueCelular;
        CmdToggleCelular(newValue);
    
        MainMenu.instance.ToggleCelular();
        celular.SetActive(newValue);
    }
    
    [Command]
    private void CmdToggleCelular(bool value)
    {
        _valueCelular = value;
        RpcToggleCelular(value);
    }
    [ClientRpc]
    private void RpcToggleCelular(bool value)
    {
        _playerScript._animator.SetBool("ActiveCelular", value);
    }


}
