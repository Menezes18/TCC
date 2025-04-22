using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{
    public PlayerControlSO playerControlSO;
    public GameObject celular;

    private PlayerScript _playerScript;
    private bool _valueCelular = false;

    private void Start()
    {
        _playerScript = GetComponent<PlayerScript>();
        celular.SetActive(false);

        if (isLocalPlayer)
        {
            playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
        }
    }

    private void EventOnCelularMenu(InputAction.CallbackContext ctx)
    {
        _valueCelular = !_valueCelular;
        CmdSetCelular(_valueCelular);
    }

    [Command]
    private void CmdSetCelular(bool activate)
    {
        RpcSetCelular(activate);
    }

    [ClientRpc]
    private void RpcSetCelular(bool activate)
    {
        celular.SetActive(activate);
        _playerScript._animator.SetBool("ActiveCelular", activate);
    }
}