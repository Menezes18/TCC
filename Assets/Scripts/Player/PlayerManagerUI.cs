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
        celular.SetActive(false);
        _playerScript = GetComponent<PlayerScript>();
        playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
    }

    private void EventOnCelularMenu(InputAction.CallbackContext obj)
    {
        Debug.LogError(this.gameObject.name);
        _valueCelular = !_valueCelular;
        MainMenu.instance.ToggleCelular();
        celular.SetActive(_valueCelular);
        _playerScript._animator.SetBool("ActiveCelular",_valueCelular);
    }
}
