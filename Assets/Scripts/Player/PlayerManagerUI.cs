using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{

    public PlayerControlSO playerControlSO;
    private PlayerScript _playerScript;
    private bool _valueCelular = false;
    private void Start()
    {
        _playerScript = GetComponent<PlayerScript>();
        playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
    }

    private void EventOnCelularMenu(InputAction.CallbackContext obj)
    {
        _valueCelular = !_valueCelular;
        MainMenu.instance.ToggleCelular();
        _playerScript._animator.SetBool("ActiveCelular",_valueCelular);
    }
}
