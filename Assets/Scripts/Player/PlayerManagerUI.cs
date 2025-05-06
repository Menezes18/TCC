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
    private PlayerInput _playerInput;
    private bool _valueCelular = false;

    private void Start()
    {
        if(!this.isOwned) return;
        PlayerControlsSO.OnMenu += EventOnCelularMenu;
        PlayerControlsSO.OnCursor += PlayerControlsSO_OnCursor;
        PlayerControlsSO_OnCursor();
        
        //Cache
        _playerScript = GetComponent<PlayerScript>();
        _playerInput  = GetComponent<PlayerInput>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // 1) Instancia e configura UI do celular
        celularInstance = Instantiate(canvasCelularPrefab);
        mainMenu = celularInstance.GetComponentInChildren<MainMenu>(true);
        celularInstance.SetActive(true);

        
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
        _playerScript._menuOpen = !_playerScript._menuOpen;
        //celularInstance.SetActive(_valueCelular);
        // animação se quiser:
        // _playerScript.Animator.SetBool("ActiveCelular", _valueCelular);
        mainMenu.ToggleCelular();
        var look = _playerInput.actions["Look"];
        Debug.Log($"[Menu] antes: Look.enabled = {look.enabled}");
        if (_playerScript._menuOpen) look.Disable(); 
        else           look.Enable();
        Debug.Log($"[Menu] depois: Look.enabled = {look.enabled}");
    }
}