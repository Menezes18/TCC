using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{

    public PlayerControlSO playerControlSO;

    private void Start()
    {
        playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
    }

    private void EventOnCelularMenu(InputAction.CallbackContext obj)
    {
        MainMenu.instance.ToggleCelular();
    }
}
