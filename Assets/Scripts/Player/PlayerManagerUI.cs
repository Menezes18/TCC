using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{
    public PlayerControlSO playerControlSO;
    public GameObject celular;

    private bool _valueCelular = false;
    private PlayerScript _playerScript;

    private void Start()
    {
        if (!isLocalPlayer) return;

        celular.SetActive(false);
        _playerScript = GetComponent<PlayerScript>();
        playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
    }

    private void EventOnCelularMenu(InputAction.CallbackContext ctx)
    {
        _valueCelular = !_valueCelular;
        CmdSetCelular(_valueCelular);
    }

    [Command]
    private void CmdSetCelular(bool activate)
    {
        _valueCelular = activate;
        RpcSetCelular(activate);
    }

    [ClientRpc]
    private void RpcSetCelular(bool activate)
    {
        celular.SetActive(activate);
        _playerScript._animator.SetBool("ActiveCelular", activate);
    }
}