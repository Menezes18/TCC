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
        if(!this.isOwned) return;
        PlayerControlsSO.OnMenu += EventOnCelularMenu;
        PlayerControlsSO.OnCursor += PlayerControlsSO_OnCursor;
        PlayerControlsSO_OnCursor();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // 1) Instancia e configura UI do celular
        celularInstance = Instantiate(canvasCelularPrefab);
        mainMenu = celularInstance.GetComponentInChildren<MainMenu>(true);
        celularInstance.SetActive(true);

        _playerScript = GetComponent<PlayerScript>();
        
    }

    
    private void OnDestroy()
    {
        if(!this.isOwned) return;
        PlayerControlsSO.OnMenu   -= EventOnCelularMenu;
        PlayerControlsSO.OnCursor -= PlayerControlsSO_OnCursor;
    }

    private void PlayerControlsSO_OnCursor()
    {
        bool novoEstado = !Cursor.visible;
        Cursor.visible = novoEstado;
        Cursor.lockState = novoEstado ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void EventOnCelularMenu()
    {
        // Segurança extra: não faz nada se mainMenu não existir
        if (mainMenu == null) return;

        _valueCelular = !_valueCelular;
        //celularInstance.SetActive(_valueCelular);
        // animação se quiser:
        // _playerScript.Animator.SetBool("ActiveCelular", _valueCelular);
        mainMenu.ToggleCelular();
    }
}