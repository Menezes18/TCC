using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{
    [Header("Referências")]
    public PlayerControlSO playerControlSO;

    [Header("Prefabs")]
    [SerializeField] private GameObject canvasCelularPrefab;  

    private GameObject celularInstance;
    private MainMenu mainMenu;
    private PlayerScript _playerScript;
    private bool _valueCelular = false;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (!isLocalPlayer) return;

        celularInstance = Instantiate(canvasCelularPrefab);

        mainMenu = celularInstance.GetComponentInChildren<MainMenu>(true);
        var tag = celularInstance.GetComponentInChildren<CelularTag>(true);
        celularInstance.SetActive(true);

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
        // Alterna estado do celular
        _valueCelular = !_valueCelular;
        // celularInstance.SetActive(_valueCelular);

        // Opcional: animação no player
        // _playerScript._animator.SetBool("ActiveCelular", _valueCelular);

        // Alterna menu principal do celular
        mainMenu.ToggleCelular();
    }

    // Caso queira notificar o servidor, descomente e adapte:
    /*
    [Command]
    private void CmdNotifyCelularState(bool active)
    {
        // lógica server-side
    }
    */
}