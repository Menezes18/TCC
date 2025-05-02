using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerManagerUI : NetworkBehaviour
{
    [Header("Referências")]
    public PlayerControlsSO PlayerControlsSO;

    [Header("Prefabs")]
    [SerializeField] private GameObject canvasCelularPrefab;  

    private GameObject celularInstance;
    private MainMenu mainMenu;
    private PlayerScript _playerScript;
    private bool _valueCelular = false;

    private void Start()
    {
        PlayerControlsSO.OnMenu += EventOnCelularMenu;
        PlayerControlsSO.OnCursor += PlayerControlsSO_OnCursor; 
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (!isLocalPlayer) return;

        celularInstance = Instantiate(canvasCelularPrefab);

        mainMenu = celularInstance.GetComponentInChildren<MainMenu>(true);
        var tag = celularInstance.GetComponentInChildren<CelularTag>(true);
        celularInstance.SetActive(true);

        _playerScript = GetComponent<PlayerScript>();
        
        
    }

    private void OnDestroy()
    {
        //if (isLocalPlayer)playerInputSo.EventOnCelularMenu -= EventOnCelularMenu;
    }
    private void PlayerControlsSO_OnCursor()
    {
        bool novoEstado = !Cursor.visible;

        Cursor.visible = novoEstado;
        Cursor.lockState = novoEstado ? CursorLockMode.None : CursorLockMode.Locked;
    }
    private void EventOnCelularMenu()
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