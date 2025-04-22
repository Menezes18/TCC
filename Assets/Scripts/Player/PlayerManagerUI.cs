using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{
    [Header("Referências")]
    public PlayerControlSO playerControlSO;
    
    private GameObject celular;
    private MainMenu mainMenu;
    private PlayerScript _playerScript;
    private bool _valueCelular = false;

    public override void OnStartLocalPlayer()
    {
        if (!isLocalPlayer) return;  

        UIManager.Instance.SpawnLocalUI();

        mainMenu = UIManager.Instance.LocalUI
            .GetComponentInChildren<MainMenu>(true);

        var tag = UIManager.Instance.LocalUI
            .GetComponentInChildren<CelularTag>(true);
        celular = tag.gameObject;

        celular.SetActive(false);
        _playerScript = GetComponent<PlayerScript>();
        playerControlSO.EventOnCelularMenu += EventOnCelularMenu;
    }

    private void OnDestroy()
    {
        if (isLocalPlayer)
            playerControlSO.EventOnCelularMenu -= EventOnCelularMenu;
    }

    private void EventOnCelularMenu(InputAction.CallbackContext ctx)
    {
        _valueCelular = !_valueCelular;

        celular.SetActive(_valueCelular);

        _playerScript._animator.SetBool("ActiveCelular", _valueCelular);

        mainMenu.ToggleCelular();
    }
}