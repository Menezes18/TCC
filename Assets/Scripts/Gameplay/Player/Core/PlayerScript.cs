using System;
using System.Collections;
using Mirror;
using Smooth;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum PlayerState{
    Default,
    Ascend,
    Descend,
    Stagger,
    Roll,
    Death,
}

public enum PlayerStatus {
    Default,
    Blinded,
    Pushing,
    ThrowPrepare,
    Throw,
}

public enum PlayerVfx
{
    Ascendfx,
    Pushingfx,

}
public class PlayerScript : NetworkBehaviour, IDamageable, IHitKillable
{
    [SerializeField] Database db;
    [SerializeField] PlayerControlsSO PlayerControlsSO;
    [SerializeField] HUDSO HUDSO;
    [SerializeField] SmoothSyncMirror _smoothSyncMirror;
    [SerializeField] private DeathEffectsSO deathEffects;
    private bool _suppressHideOnDeath; 

    [SerializeField] CharacterController _controller;
    [SerializeField] Animator _animator;
    [SerializeField] NetworkAnimator _networkAnimator;

    [SerializeField] PlayerState _state;
    public PlayerState State {
        get { return _state; }
        set {
            if (_state == value) { return; }
            OnStateChanged(_state, value);
            _animator.SetInteger(_STATE, (int)value);
            _state = value;
        }
    }

    [SerializeField] public PlayerStatus _status;

    public PlayerStatus Status {
        get { return _status; }
        set {
            if (_status == value) return;

            Debug.Log($"🔄 [STATUS] {_status} → {value}");
            _animator.SetInteger(_STATUS, (int)value);
            _status = value;

            if (value == PlayerStatus.Throw) {
                _animator.SetTrigger("throw");
                _networkAnimator.SetTrigger("throw");
            }

            if (value != PlayerStatus.Pushing) return;

            // NetworkAnimator não replica trigger
            // Então tem que passar sempre nos 2
            // animator --> trigger
            // networkAnimator --> trigger
            _animator.SetTrigger("push");
            _networkAnimator.SetTrigger("push");

        }
    }
    public bool IsDead => State == PlayerState.Death;

    public Transform _cam;

    public Vector3 rot => new Vector3(0, _cam.transform.rotation.eulerAngles.y, 0);

    private Vector3 _input;
    private Vector3 _raw;

    private Vector3 _move;
    private Vector3 _inertia;


    private float _yaw;
    private float _pitch;
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private float shootOffset = 0.5f;
    [SerializeField] public Transform _staggerIndicator;
    [SerializeField] public Transform _bostaIndicator;
    [SerializeField] public GameObject _gameObjectBosta;

    private float _inertiaCap;
    private float InertiaCap {
        get { return _inertiaCap; }
        set {
            _inertiaCap = Mathf.Clamp(value, db.playerSpeed, db.playerMaxAirSpeed);
        }
    }

    private Vector3 _roll;

    private float _mouseX, _mouseY;
    private bool _ignoreGroundedNextFrame;

    readonly int _STATE = Animator.StringToHash("state");
    readonly int _STATUS = Animator.StringToHash("status");
    readonly int _MOVEX = Animator.StringToHash("MoveX");
    readonly int _MOVEY = Animator.StringToHash("MoveY");
    readonly int _DEATHCAUSE = Animator.StringToHash("deathCause");

    private float _staggerTimer;
    private float _pushCooldown;
    private float _rollTimer;
    private float _rollCooldown;
    private float _blindTimer;
    private float _poopSlowTimer;
    private float _throwCooldown;
    private float _groundSnapLockTimer; // evita clamp vertical logo após impulso

    public float PushCooldownNormalized => Mathf.Clamp01(_pushCooldown / db.playerPushCooldownTimer);
    public float ThrowCooldownNormalized => Mathf.Clamp01(_throwCooldown / db.playerThrowCooldown);
    private float BlindTimer {
        get => _blindTimer;
        set {
            if (_blindTimer == value) return;
            _blindTimer = value;
        }
    }


    private PlayerInput _playerInput;

    [Header("Input Schemes")]
    [SerializeField] private string keyboardMouseScheme = "Keyboard&Mouse";
    [SerializeField] private string gamepadScheme = "Gamepad";

    public bool IsAirborne => State == PlayerState.Ascend || State == PlayerState.Descend;
    [SyncVar(hook = nameof(OnExtraFreezeChanged))]
    public bool _extraFreeze;
    public bool isFrozen
    {
        get => (MatchManager.singleton != null && MatchManager.singleton.Freeze) || _extraFreeze;
        [Server]
        set => _extraFreeze = value;
    }


    public Transform cameraTarget;

    [Header("Panel Camera")]
    [SerializeField] private PanelCameraSO panelCamera;
    private bool _lastPanelState;
    private float _panelExitTimer;
    private float _savedYawBeforePanel, _savedPitchBeforePanel;
    private float _panelFixedYaw, _panelFixedPitch;
    private float _panelRotateX;
    private float _panelZoomOffset = 0f; 
    [SerializeField] private float panelZoomSpeed = 0.5f;
    [SerializeField] private float panelZoomMin = -1.5f;
    [SerializeField] private float panelZoomMax = 1.5f;
    private Transform _panelAnchor;

    public void SetPanelCameraAnchor(Transform anchor) { _panelAnchor = anchor; }
    public void ClearPanelCameraAnchor() { _panelAnchor = null; }

    [SyncVar(hook = nameof(OnStaggerChanged))]
    private bool isStaggered;
    
    [SyncVar(hook = nameof(OnBlindedChanged))]
    private bool isBlinded;

    private float _lastPredictedImpulseTime = -999f;
    private const float PredictedImpulseReconcileWindow = 0.15f;

    private bool _menuOpen = false;
    private float _nextMenuToggleTime = 0f;
    public bool panel = false;
    [SerializeField] private bool _uiLocked = false;
    public bool UILocked { get => _uiLocked; set => _uiLocked = value; }
    [SerializeField] private bool _chatOpen = false;

    [SerializeField] private float sensibilidade = 1;
    [SyncVar(hook = nameof(OnCarryingChanged))] private bool _isCarrying;
    [SyncVar(hook = nameof(OnHotPotatoChanged))] private bool _isHotPotatoHolder;
    [SyncVar(hook = nameof(OnBoostChanged))] private float _boostSpeedMultiplier = 1f;
    [SerializeField, Range(0.3f, 1f)] private float carryingSpeedMultiplier = 0.8f; 
    [SerializeField, Range(0.3f, 1f)] private float poopSpeedMultiplier = 0.5f;
    public bool IsCarrying => _isCarrying;
    
    [SyncVar] private bool _isAirborneServer;
    private bool _lastAirborneSent;

    // Forças externas de solo (ex.: esteira)
    private Vector3 _externalGroundVelocity; 
    private float _externalGroundTimer;      

    // Redução de controle (gelo)
    private float _controlMultiplier = 1f;  
    private float _controlTimer;

    // UI

    [Header("Prefabs")]
    [SerializeField] private GameObject canvasCelularPrefab;
    private GameObject celularInstance;
    public MainMenu mainMenu;


    
    [Header("Minigame Street - Banana")]
    [SerializeField] private Transform bananaAttachPoint; // Ponto nas costas do jogador
    private GameObject bananaInstance;

    [Header("Minigame Hot Potato")]
    private GameObject potatoInstance;

    // Spectator System
    [Header("Spectator")]
    [SerializeField] private GameObject playerModelRoot; 
    private bool _isSpectating = false;
    private List<PlayerScript> _alivePlayersCache = new List<PlayerScript>();
    private int _currentSpectatorIndex = 0;
    public bool IsSpectating => _isSpectating;
    public PlayerScript CurrentSpectatedTarget { get; private set; }

    // Event
    public UnityEvent EventOnDeath;
    public UnityEvent EventOnDeathServerSide;
    public UnityEvent EventOnRespawn;
    public UnityEvent EventOnJump;
    public UnityEvent EventOnPush;

    private void Awake()
    {
        if (cameraTarget == null)
            cameraTarget = transform;
    }

    private void Start()
    {
        if (!isLocalPlayer) return;

        PlayerControlsSO.OnMove += PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook += PlayerControlsSO_OnLook;
        PlayerControlsSO.OnJump += PlayerControlsSO_OnJump;
        PlayerControlsSO.OnPush += PlayerControlsSO_OnPush;
        PlayerControlsSO.OnRoll += PlayerControlsSO_OnRoll;
        PlayerControlsSO.OnThrow += PlayerControlsSO_OnThrow;
        PlayerControlsSO.OnThrowCancel += PlayerControlsSO_OnThrowCancel;
        PlayerControlsSO.OnDebug += PlayerControlsSOOnOnDebug;
        PlayerControlsSO.OnRotatePanel += PlayerControlsSO_OnRotatePanel;
        PlayerControlsSO.OnZoomPanel += PlayerControlsSO_OnZoomPanel;
        PlayerControlsSO.OnClosePanel += PlayerControlsSO_OnClosePanel;

        // Spectator controls (using roll for next and push for previous while dead)
        // We'll handle spectator switching in Update based on state

        //
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (_cam == null)
        {
            if (Camera.main != null) _cam = Camera.main.transform;
            else Debug.LogWarning("[PlayerScript] No main camera found to assign as _cam.");
        }

        _playerInput = GetComponent<PlayerInput>();
        // Não desabilitar o NetworkAnimator no local player. Deixe sempre habilitado para sincronizar parâmetros.

        ConfigureControlScheme(initial:true);

        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            sensibilidade = PlayerPrefs.GetFloat("MouseSensitivity");
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (base.isOwned == false) return;
        // NetworkAnimator deve permanecer habilitado; não alternar aqui
        ConfigureControlScheme(initial:false);
        PlayerControlsSO.OnMenu += EventOnCelularMenu;
        if (_cam == null)
        {
            if (Camera.main != null) _cam = Camera.main.transform;
        }
        // UI
        celularInstance = Instantiate(canvasCelularPrefab);
        mainMenu = celularInstance.GetComponentInChildren<MainMenu>(true);
        celularInstance.SetActive(false);
        // Unifica HUDSO nos painéis do celular (minigame / cores)
        if (celularInstance != null && HUDSO != null)
        {
            var colorPanel = celularInstance.GetComponentInChildren<ColorChangePanel>(true);
            if (colorPanel != null) colorPanel.SetHUD(HUDSO);
            
            var minigamePanel = celularInstance.GetComponentInChildren<MinigameSelectionPanel>(true);
            if (minigamePanel != null) minigamePanel.SetHUD(HUDSO);
            
            if (mainMenu != null) mainMenu.SetHUD(HUDSO);
        }

        
        StartCoroutine(ApplyPlayerCustomizationDelayed());
        
        HUDSO.EventOnShowMenuPanel += OnShowMenuPanel;
        HUDSO.EventOnHideMenuPanel += OnHideMenuPanel;
    }
    
    private IEnumerator ApplyPlayerCustomizationDelayed()
    {
        yield return new WaitForSeconds(0.2f);
        ApplyPlayerCustomization();
        
        yield return new WaitForSeconds(0.5f);
        ApplyPlayerCustomization();
    }
    
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        PlayerControlsSO.OnMenu -= EventOnCelularMenu;
       
        HUDSO.EventOnShowMenuPanel -= OnShowMenuPanel;
        HUDSO.EventOnHideMenuPanel -= OnHideMenuPanel;
    }

    private void ConfigureControlScheme(bool initial)
    {
        if (_playerInput == null) return;
        _playerInput.neverAutoSwitchControlSchemes = false;
        // choose a scheme based on devices the user currently has
        try
        {
            if (Gamepad.current != null)
            {
                _playerInput.SwitchCurrentControlScheme(gamepadScheme, Gamepad.current);
                return;
            }

            var devices = new System.Collections.Generic.List<InputDevice>();
            if (Keyboard.current != null) devices.Add(Keyboard.current);
            if (Mouse.current != null) devices.Add(Mouse.current);

            if (devices.Count > 0)
            {
                _playerInput.SwitchCurrentControlScheme(keyboardMouseScheme, devices.ToArray());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerScript] Failed to switch control scheme: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (!this.isOwned) return;
        
        // Limpa a banana se existir
        HideBanana();
        
        // Limpa a batata se existir
        HidePotato();
        
        PlayerControlsSO.OnMove -= PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook -= PlayerControlsSO_OnLook;
        PlayerControlsSO.OnJump -= PlayerControlsSO_OnJump;
        PlayerControlsSO.OnPush -= PlayerControlsSO_OnPush;
        PlayerControlsSO.OnRoll -= PlayerControlsSO_OnRoll;
        PlayerControlsSO.OnThrow -= PlayerControlsSO_OnThrow;
        PlayerControlsSO.OnThrowCancel -= PlayerControlsSO_OnThrowCancel;
        PlayerControlsSO.OnRotatePanel -= PlayerControlsSO_OnRotatePanel;
        PlayerControlsSO.OnZoomPanel -= PlayerControlsSO_OnZoomPanel;
        PlayerControlsSO.OnClosePanel -= PlayerControlsSO_OnClosePanel;

        //UI
        PlayerControlsSO.OnMenu -= EventOnCelularMenu;

    }

    private void Update()
    {
        if (!this.isOwned) return;
        if (!isLocalPlayer) return;
        //
        if (_pushCooldown > 0) _pushCooldown -= Time.deltaTime;

        if (_staggerTimer > 0) _staggerTimer -= Time.deltaTime;

        if (_rollTimer > 0) _rollTimer -= Time.deltaTime;

        if (_rollCooldown > 0) _rollCooldown -= Time.deltaTime;

        if (_throwCooldown > 0) _throwCooldown -= Time.deltaTime;

        // Atualiza cooldown UI via HUDSO
        HUDSO.UpdatePushCooldown(PushCooldownNormalized);
        HUDSO.UpdateThrowCooldown(ThrowCooldownNormalized);

        if (_blindTimer > 0) _blindTimer -= Time.deltaTime;
        if (_poopSlowTimer > 0f)
        {
            _poopSlowTimer -= Time.deltaTime;
            if (_poopSlowTimer < 0f) _poopSlowTimer = 0f;
        }
        if (_groundSnapLockTimer > 0f) _groundSnapLockTimer -= Time.deltaTime;

        if (_externalGroundTimer > 0f)
        {
            _externalGroundTimer -= Time.deltaTime;
            if (_externalGroundTimer <= 0f)
            {
                _externalGroundTimer = 0f;
                _externalGroundVelocity = Vector3.zero;
            }
        }

        if (_controlTimer > 0f)
        {
            _controlTimer -= Time.deltaTime;
            if (_controlTimer <= 0f)
            {
                _controlTimer = 0f;
                _controlMultiplier = 1f;
            }
        }
        
        // Spectator controls - cycle through alive players
        if (_isSpectating)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                CycleToPreviousSpectatorTarget();
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                CycleToNextSpectatorTarget();
            }
        }

        if (!UILocked && Keyboard.current.pKey.wasPressedThisFrame ) // input
        {
            // Bloqueia alternar "pronto" enquanto o briefing não liberar interação
            if (BriefingManager.singleton != null && !BriefingManager.singleton.ReadyInteractableClient)
            {
                Debug.Log("[Ready] Ignorado: aguardando todos entrarem no briefing");
            }
            else
            {
                NetworkClient.localPlayer.GetComponent<PlayerData>().ToggleReady();
            }
            
            Scene sceneAtual = SceneManager.GetActiveScene();
            if (sceneAtual.name == "RASCUNHO"){
                
                LeanTween.delayedCall(1f, () => {
                    if(MainMenu.instance == null) return;
                    MainMenu.instance.StartGame();
                });
            }
            else{
                Debug.Log("🧭 [BRIEFING] BriefingManager");
                BriefingManager.singleton?.CheckAllReady();
            }
        }
        // if (!UILocked && Keyboard.current.oKey.wasPressedThisFrame )
        // {
        //     HUDSO.ShowColorChangePanel(); 
        // }
        float blindWeight = CustomMath.ConvertRange(_blindTimer, db.playerBlindDuration, 0);
        float blindRange = db.playerBlindCurve.Evaluate(blindWeight);
        HUDSO.SetBlindAlpha(blindRange);

        AerialDetection();


        /* if (State == PlayerState.Roll){
             float vertical = _move.y;

             float weight = CustomMath.Normalized01(_rollTimer, db.playerMaxAirSpeed, 0f);
             float range = db.playerRollCurve.Evaluate(weight);
             Debug.Log(weight + " -> " + range);
             Vector3 horizontal = _roll;
             horizontal = Quaternion.Euler(rot) * horizontal;
             horizontal *= db.playerRollSpeed * range;

             _move = horizontal;

             _move.y = vertical;

             if (_rollTimer <= 0) State = GetDefaultStatus();
         }*/

        if (Status == PlayerStatus.Throw) {
            if (_throwCooldown <= 0) {

                Status = PlayerStatus.Default;
            }
        }
        if (Status == PlayerStatus.Blinded) {
            if (_blindTimer <= 0f) {
                _blindTimer = 0f;
                Status = PlayerStatus.Default;
                
                // Desativa o estado blinded sincronizado (remove indicador e bosta)
                if (isLocalPlayer && isOwned)
                {
                    CmdSetBlinded(false);
                }
            }
        }

        StaggerBehaviour();
        AerialBehaviour();
        DefaultBehaviour();

        // Zera parâmetros de movimento do Animator enquanto o chat está aberto
        if (_chatOpen)
        {
            _animator.SetFloat(_MOVEX, 0f, 0.1f, Time.deltaTime);
            _animator.SetFloat(_MOVEY, 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            _animator.SetFloat(_MOVEX, _input.x, 0.1f, Time.deltaTime);
            _animator.SetFloat(_MOVEY, _input.z, 0.1f, Time.deltaTime);
        }

        aimWeigh = CustomMath.ConvertRange(_pitch, db.maxMouseX, db.minMouseY);
        _animator.SetFloat("animweight", aimWeigh);

        _move = _move + Vector3.up * db.gravity * Time.deltaTime;

        if (State != PlayerState.Death) {
            _controller.Move(_move * Time.deltaTime);
        }

        if (_controller.isGrounded && _groundSnapLockTimer <= 0f) {
            _move.y = db.gravityGrounded;
        }

        if (!panel)
        {
            transform.rotation = Quaternion.Euler(rot);
        }
        else
        {
            transform.Rotate(0f, _panelRotateX * GetPanelRotateSpeed() * Time.deltaTime, 0f);
        }

    }
    private void LateUpdate()
    {
        if (!this.isOwned) return;

        if (panel != _lastPanelState)
        {
            if (panel)
            {
                _savedYawBeforePanel = _yaw;
                _savedPitchBeforePanel = _pitch;
                Vector3 anchorPos = (_panelAnchor != null ? _panelAnchor.position : cameraTarget.position);
                Vector3 flatDir = transform.position - anchorPos; flatDir.y = 0f;
                if (flatDir.sqrMagnitude < 0.0001f) flatDir = transform.forward;
                _panelFixedYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
                _panelFixedPitch = Mathf.Clamp(GetPanelPitch(), db.minMouseY, db.maxMouseX);
                _panelExitTimer = 0f;
            }
            else
            {
                _panelExitTimer = 0f;
                _panelRotateX = 0f;
                _panelZoomOffset = 0f; 
            }
            _lastPanelState = panel;
        }

        Transform anchorRef = (panel && _panelAnchor != null) ? _panelAnchor : cameraTarget;

        if (panel)
        {
            _yaw = Mathf.LerpAngle(_yaw, _panelFixedYaw, Time.deltaTime * GetPanelLerp());
            _pitch = Mathf.Lerp(_pitch, _panelFixedPitch, Time.deltaTime * GetPanelLerp());
        }
        else if (_panelExitTimer < GetPanelExitDuration())
        {
            _yaw = Mathf.LerpAngle(_yaw, _savedYawBeforePanel, Time.deltaTime * GetPanelLerp());
            _pitch = Mathf.Lerp(_pitch, _savedPitchBeforePanel, Time.deltaTime * GetPanelLerp());
            _panelExitTimer += Time.deltaTime;
        }

        Quaternion camRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        _cam.rotation = camRotation;

        Vector3 offset;
        if (panel)
        {
            offset = GetPanelOrbitalOffset();
            offset.z += _panelZoomOffset;
        }
        else if (_panelExitTimer < GetPanelExitDuration())
        {
            float t = Mathf.Clamp01(_panelExitTimer / GetPanelExitDuration());
            offset = Vector3.Lerp(GetPanelOrbitalOffset(), db.orbitalOffset, t);
            float zoomTransition = Mathf.Lerp(_panelZoomOffset, 0f, t);
            offset.z += zoomTransition;
        }
        else
        {
            offset = db.orbitalOffset;
        }

        Vector3 desiredPos = anchorRef.position + camRotation * offset;
        Vector3 dir = desiredPos - anchorRef.position;
        float maxDist = offset.magnitude;

        if (Physics.SphereCast(anchorRef.position, db.cameraSphereRadius, dir.normalized,
                out RaycastHit hit, maxDist, db.cameraColliderMash,
                QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Clamp(hit.distance - db.cameraSphereRadius, 0.1f, maxDist);
            _cam.transform.position = anchorRef.position + dir.normalized * safeDist;
        }
        else
        {
            _cam.transform.position = desiredPos;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("KillPlane"))
        {
            OnContextualHit(DeathCause.Default, false);
        }
    }

    private void PlayerControlsSO_OnRotatePanel(float x)
    {
        _panelRotateX = x;
    }
    
    private void PlayerControlsSO_OnZoomPanel(float scroll)
    {
        _panelZoomOffset += scroll * panelZoomSpeed;
        _panelZoomOffset = Mathf.Clamp(_panelZoomOffset, panelZoomMin, panelZoomMax);
    }
    
    private void PlayerControlsSO_OnClosePanel()
    {
        if (!isOwned) return;

        if (panel)
        {
            HUDSO.HideColorChangePanel();
            HUDSO.HideMinigameSelectionPanel();
            return;
        }
        
    }

    private Vector3 GetPanelOrbitalOffset() => panelCamera != null ? panelCamera.panelOrbitalOffset : new Vector3(0.12f, 0.08f, -2.29f);
    private float GetPanelLerp() => panelCamera != null ? panelCamera.panelCamLerp : 6f;
    private float GetPanelPitch() => panelCamera != null ? panelCamera.panelPitch : 8f;
    private float GetPanelExitDuration() => panelCamera != null ? panelCamera.panelExitDuration : 0.5f;
    private float GetPanelRotateSpeed() => panelCamera != null ? panelCamera.panelRotateSpeed : 300f;
    
    [Server] public void ServerSetCarrying(bool value) { _isCarrying = value; }

    [Server] public void ServerSetHotPotatoHolder(bool active) { _isHotPotatoHolder = active; }
    [Server] public void ServerSetBoostMultiplier(float multiplier) { _boostSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f); }
    [Server] public void ServerClearBoost() { _boostSpeedMultiplier = 1f; }
    
    private void OnCarryingChanged(bool oldVal, bool newVal) 
    { 
        // Sincroniza a visualização da banana em todos os clientes
        if (newVal)
            ShowBanana();
        else
            HideBanana();
    }
    
    private void ShowBanana()
    {
        // Se já existe, não criar outro
        if (bananaInstance != null) return;
        
        // Verifica se tem o prefab configurado no Database
        if (db == null || db.streetBananaPrefab == null)
        {
            Debug.LogWarning("⚠️ [PlayerScript] Banana prefab não configurado no Database!");
            return;
        }
        
        Transform parent = bananaAttachPoint != null ? bananaAttachPoint : transform;
        
        bananaInstance = Instantiate(db.streetBananaPrefab, parent);
        
        var pd = GetComponent<PlayerData>();
        string playerName = pd?.playerInfo.username ?? "jogador";
        Debug.Log($"🍌 [PlayerScript] Banana mostrada para {playerName}");
    }
    
    private void HideBanana()
    {
        if (bananaInstance != null)
        {
            Destroy(bananaInstance);
            bananaInstance = null;
            
            var pd = GetComponent<PlayerData>();
            string playerName = pd?.playerInfo.username ?? "jogador";
            Debug.Log($"🍌 [PlayerScript] Banana escondida para {playerName}");
        }
    }

    private void OnHotPotatoChanged(bool oldVal, bool newVal)
    {
        if (newVal)
            ShowPotato();
        else
            HidePotato();
    }

    private void ShowPotato()
    {
        if (potatoInstance != null) return;
        
        if (db == null || db.hotPotatoPrefab == null)
        {
            Debug.LogWarning("⚠️ [PlayerScript] Potato prefab não configurado no Database!");
            return;
        }
        
        Transform attachPoint = transform.Find("SpawnPoint");
        
        Transform parent = attachPoint != null ? attachPoint : transform;
        
        potatoInstance = Instantiate(db.hotPotatoPrefab, parent);
        potatoInstance.transform.localScale = Vector3.one * 0.1f;
        var pd = GetComponent<PlayerData>();
        string playerName = pd?.playerInfo.username ?? "jogador";
        Debug.Log($"🥔 [PlayerScript] Batata mostrada para {playerName}");
    }
    
    private void HidePotato()
    {
        if (potatoInstance != null)
        {
            Destroy(potatoInstance);
            potatoInstance = null;
            
            var pd = GetComponent<PlayerData>();
            string playerName = pd?.playerInfo.username ?? "jogador";
            Debug.Log($"🥔 [PlayerScript] Batata escondida para {playerName}");
        }
    }
    
    private void OnBoostChanged(float oldVal, float newVal) { }
    private float GetSpeedMultiplier()
    {
        float mult = 1f;
        float carryMul = db != null ? Mathf.Clamp(db.playerCarryingSpeedMultiplier, 0.1f, 1f) : carryingSpeedMultiplier;
        float poopMul = db != null ? Mathf.Clamp(db.playerPoopSpeedMultiplier, 0.1f, 1f) : poopSpeedMultiplier;
        if (_isCarrying) mult *= carryMul;
        if (Status == PlayerStatus.Blinded && _poopSlowTimer > 0f) mult *= poopMul;
        if (_isHotPotatoHolder) mult *= GetConfiguredHotPotatoMultiplier();
        mult *= Mathf.Max(0.1f, _boostSpeedMultiplier);
        return Mathf.Max(0.05f, mult);
    }

    public float GetConfiguredHotPotatoMultiplier()
    {
        return db != null ? Mathf.Clamp(db.hotPotatoHolderSpeedMultiplier, 1f, 3f) : 1.25f;
    }
    public float GetConfiguredCarryingMultiplier()
    {
        return db != null ? Mathf.Clamp(db.playerCarryingSpeedMultiplier, 0.1f, 1f) : carryingSpeedMultiplier;
    }


    //
    private void AerialDetection()
    {
        if (State == PlayerState.Death) return;
        if (State == PlayerState.Stagger) return;
        if (State == PlayerState.Roll) return;

        if (_move.y > 0)
            State = PlayerState.Ascend;
        else if (_move.y < db.gravityGrounded)
            State = PlayerState.Descend;

        if (_ignoreGroundedNextFrame == true) {
            _ignoreGroundedNextFrame = false;
            return;
        }

        if (_controller.isGrounded == true) {
            State = PlayerState.Default;
        }
        
        // Atualiza flag de "no ar" para o servidor, quando for o dono local
        bool airborneNow = (State == PlayerState.Ascend || State == PlayerState.Descend);
        if (isLocalPlayer && this.isOwned)
        {
            if (_lastAirborneSent != airborneNow)
            {
                _lastAirborneSent = airborneNow;
                CmdSetAirborne(airborneNow);
            }
        }
    }
    private void StaggerBehaviour()
    {
        if (State != PlayerState.Stagger) return;



        float vertical = _move.y;

        Vector3 input = new Vector3(_input.x, 0, _input.z);
        input = Quaternion.Euler(rot) * input;

        //
        if (_staggerTimer > 0) input = Vector3.zero;

        float airSpeed = db.playerAirSpeed * db.playerStaggerAirSpeedModifier;

        input *= airSpeed * Time.deltaTime;

        _inertia += input;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;

        if (_staggerTimer > 0) return;

        // Exit Condition
        if (_controller.isGrounded == false) return;



        State = PlayerState.Default;
        if (isLocalPlayer && isOwned)
        {
            CmdSetStaggered(false);
        }


    }
    private void AerialBehaviour()
    {

        if (State != PlayerState.Ascend && State != PlayerState.Descend) return;

        float vertical = _move.y;

        Vector3 input = new Vector3(_input.x, 0, _input.z);
        input = Quaternion.Euler(rot) * input;
        input *= (db.playerAirSpeed * GetSpeedMultiplier()) * Time.deltaTime;
        _inertia += input;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;

    }
    private void DefaultBehaviour()
    {
        if (State != PlayerState.Default) return;
        float vertical = _move.y;

        _move = _input;
        // reduz o controle (ex.: gelo)
        _move *= Mathf.Clamp01(_controlMultiplier);
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed * GetSpeedMultiplier();

        // adiciona força externa de solo (ex.: esteira) – somente horizontal
        _move += new Vector3(_externalGroundVelocity.x, 0f, _externalGroundVelocity.z);

        _move.y = vertical;

        _move += Vector3.up * db.gravity;
    }

    //
    public void SetDefaultState()
    {
        if (!_controller.isGrounded) {
            if (_move.y > 0)
                State = PlayerState.Ascend;
            else
                State = PlayerState.Descend;
        }
        State = PlayerState.Default;
    }
    public void SetStatusDefault()
    {
        Status = PlayerStatus.Default;
    }

    public PlayerState GetDefaultStatus()
    {
        if (_move.y > 0)
            return PlayerState.Ascend;
        if (_move.y < -1)
            return PlayerState.Descend;

        return PlayerState.Default;
    }

    //
    private void PlayerControlsSO_OnMove(Vector2 input, Vector2 raw)
    {
        if (_chatOpen) { _raw = Vector3.zero; _input = Vector3.zero; return; }
        _raw = raw;
        _input = new Vector3(input.x, 0, input.y);
    }
    private void PlayerControlsSO_OnLook(Vector2 obj)
    {

        if (panel) return;
        if (_chatOpen) return;
        if (Cursor.visible == true) return;
        if (!this.isOwned) return;
        float sensitivityFactor = sensibilidade;

        _yaw += obj.x * sensitivityFactor * 0.01f;
        _pitch -= obj.y * sensitivityFactor * 0.01f;

        _pitch = Mathf.Clamp(_pitch, db.minMouseY, db.maxMouseX);



    }

    public float aimWeigh;
    private void PlayerControlsSO_OnJump()
    {
        if (panel) return;
        if (_chatOpen) return;
        if (isFrozen) return;
        if (_isCarrying) return;
        if (State != PlayerState.Default) return;

        State = PlayerState.Ascend;

        _ignoreGroundedNextFrame = true;
        _move.y = db.playerJumpHeight;
        _inertia = new Vector3(_move.x, 0, _move.z);
        InertiaCap = _inertia.magnitude;

    }
    private void PlayerControlsSO_OnPush()
    {
        // Bloqueia qualquer interação/empurrão durante espectador ou morte
        if (_isSpectating) return;
        if (State == PlayerState.Death) return;

        var rangeInteractor = GetComponent<RangeInteractor>();
        if (rangeInteractor != null && rangeInteractor.TryInteract()) return;
        if (panel) return;
        if (_chatOpen) return;
        if (_isCarrying) return;
        if (isFrozen) return;
        if (State == PlayerState.Stagger) return;
        if (Status == PlayerStatus.Pushing) return;
        if (Status == PlayerStatus.ThrowPrepare) return; 
        if (Status == PlayerStatus.Throw) return;
        if (_pushCooldown > 0) return;

        Status = PlayerStatus.Pushing;
        _pushCooldown = db.playerPushCooldownTimer;

    }
    private void PlayerControlsSO_OnRoll()
    {
        if (isFrozen) return;
        if (panel) return;
        if (_chatOpen) return;
        if (IsAirborne) return;
        if (State == PlayerState.Stagger) return;
        if (_rollCooldown > 0) return;
        if (_isCarrying) return;

        if (_roll.magnitude == 0)
            _roll = Vector3.forward;

        State = PlayerState.Roll;
        _rollTimer = db.playerRollDuration;

    }
    private void PlayerControlsSO_OnThrow()
    {
        if (isFrozen) return;
        if (panel) return;
        if (_chatOpen) return;
        if (Cursor.visible == true) return;
        if (State == PlayerState.Death) return;
        if (State == PlayerState.Stagger) return;
        if (Status == PlayerStatus.Pushing) return;
        if (Status == PlayerStatus.ThrowPrepare) return;
        if (Status == PlayerStatus.Throw) return;
        if (_throwCooldown > 0) return;
        if (_isCarrying) return;
        Status = PlayerStatus.ThrowPrepare;

    }

    private void PlayerControlsSO_OnThrowCancel()
    {
        if (isFrozen) return;
        if (panel) return;
        if (_chatOpen) return;
        if (Cursor.visible == true) return;
        if (State == PlayerState.Death) return;
        if (State == PlayerState.Stagger) return;
        if (Status == PlayerStatus.Pushing) return;
        if (Status == PlayerStatus.Throw) return;
        if (_isCarrying) return;

        Status = PlayerStatus.Throw;

        if (isFrozen) return;
        if (_isCarrying) return; 
        Vector3 direction = _cam.forward;
        // Vector3 origin = transform.TransformPoint(db.projectileLocalOffset);
        //direction = _cam.forward;

        // PrefabInstancer.singleton.CmdSpawnProjectile(
        //     origin.transform.position,
        //    direction,
        //     this.netIdentity
        // );
        _throwCooldown = db.playerThrowCooldown;
    }
    public GameObject origin;
    public void PrefabFrameInstancer()
    {
        // //Vector3 origin = transform.TransformPoint(db.projectileLocalOffset);
        if (_cam == null)
        {
            Debug.LogWarning("[PlayerScript] _cam not assigned; cannot spawn projectile.");
            return;
        }
        Vector3 direction = _cam.forward;
        //
        PrefabInstancer.singleton.CmdSpawnProjectile(
            origin.transform.position,
            direction,
            this.netIdentity
        );

        Debug.Log("✅ [SPAWN] Teste: instanciar Player");
        // _throwCooldown = db.playerThrowCooldown;
    }

    // Chamado pelo Chat UI quando abrir/fechar o campo de texto
    public void OnChatOpen()
    {
        _chatOpen = true;
        _input = Vector3.zero;
        _raw = Vector3.zero;
        _move = new Vector3(0, _move.y, 0); // preserva componente vertical
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OnChatClose()
    {
        _chatOpen = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // MUDAR ISSO AQUI DEPOIS
    private void PlayerControlsSOOnOnDebug()
    {
        if (!base.isServer) return;
        //LobbyController.singleton.
        //LobbyController.singleton.CmdPrepareMath();
    }
    //
    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        Debug.Log($"🔁 [STATE] {oldState} → {newState}");

        if (oldState == PlayerState.Roll)
            _rollCooldown = db.playerRollCooldownDuration;
    }

    //
    public void PassEvent(PlayerVfx fxState) {
        if (fxState == PlayerVfx.Ascendfx) OnEventPass(this.EventOnJump);
        if (fxState == PlayerVfx.Pushingfx) OnEventPass(this.EventOnPush);
    }
    private void OnEventPass(UnityEvent unityEventAct)
    {
        unityEventAct?.Invoke();
    }
    [Server]
    public void ReceiveDamage(DamageType dmgType, Vector3 dir)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        // Only flag stagger when the incoming damage will actually stagger the player
        if (dmgType == DamageType.Push)
            isStaggered = true;
        if (dmgType == DamageType.Poop)
            isBlinded = true;
        TargetRpcReceiveDamage(coon, dmgType, dir);
    }

    [TargetRpc]
    public void TargetRpcReceiveDamage(NetworkConnection coon, DamageType dmgType, Vector3 dir)
    {
        if (dmgType == DamageType.Poop) {
            Status = PlayerStatus.Blinded;
            _blindTimer = db.playerBlindDuration;
            float slowDuration = db != null ? Mathf.Max(0f, db.playerPoopSlowDuration) : 0f;
            if (slowDuration <= 0f && db != null)
                slowDuration = Mathf.Max(0f, db.playerBlindDuration);
            _poopSlowTimer = slowDuration;
            // O servidor já ativou isBlinded = true, então o hook OnBlindedChanged será chamado automaticamente
            return;
        }



        //

        State = PlayerState.Stagger;

        //isStaggered = true;

        Debug.DrawRay(transform.position, dir * 5, Color.cyan, 5);


        Vector3 horizontal = new Vector3(_move.x, 0, _move.z);
        Vector3 final = dir.normalized * db.playerPushStrength;
        _inertia = final;
        InertiaCap = final.magnitude;
        _move.y = db.playerStaggerHeight;
        _staggerTimer = db.playerStaggerStunDuration;

        //StartCoroutine(ClearStagger(db.playerStaggerStunDuration));

    }

    [Server]
    public void ServerApplyImpulse(Vector3 horizontalDir, float horizontalStrength, float verticalStrength, float stunDuration = 0f, bool setStagger = true)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        if (setStagger) isStaggered = true;
        TargetRpcApplyImpulse(coon, horizontalDir, horizontalStrength, verticalStrength, stunDuration, setStagger);
    }

    [TargetRpc]
    public void TargetRpcApplyImpulse(NetworkConnection coon, Vector3 horizontalDir, float horizontalStrength, float verticalStrength, float stunDuration, bool setStagger)
    {

        if (this.isOwned && isLocalPlayer)
        {
            if (Time.time - _lastPredictedImpulseTime <= PredictedImpulseReconcileWindow)
            {
                return;
            }
        }

        ApplyImpulseLocal(horizontalDir, horizontalStrength, verticalStrength, stunDuration, setStagger);
    }

    public void ApplyImpulseLocal(Vector3 horizontalDir, float horizontalStrength, float verticalStrength, float stunDuration, bool setStagger)
    {
        if (setStagger)
            State = PlayerState.Stagger;

        Vector3 h = horizontalDir.sqrMagnitude > 0f ? horizontalDir.normalized * Mathf.Max(0f, horizontalStrength) : Vector3.zero;
        _inertia = h;
        InertiaCap = h.magnitude;
        _move.y = Mathf.Max(_move.y, verticalStrength);
        _ignoreGroundedNextFrame = true;
        _groundSnapLockTimer = Mathf.Max(_groundSnapLockTimer, 0.1f);
        if (stunDuration > 0f)
            _staggerTimer = Mathf.Max(_staggerTimer, stunDuration);

    }

    public void MarkPredictedImpulse()
    {
        if (!isLocalPlayer || !this.isOwned) return;
        _lastPredictedImpulseTime = Time.time;
    }

    public bool IsAirborneServerFlag => _isAirborneServer;

    [Command]
    private void CmdSetAirborne(bool airborne)
    {
        _isAirborneServer = airborne;
    }

    [Server]
    public void ServerSetExternalGroundVelocity(Vector3 velocity, float duration, bool additive)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        TargetRpcSetExternalGroundVelocity(coon, velocity, duration, additive);
    }

    [TargetRpc]
    public void TargetRpcSetExternalGroundVelocity(NetworkConnection coon, Vector3 velocity, float duration, bool additive)
    {
        if (additive)
            _externalGroundVelocity += velocity;
        else
            _externalGroundVelocity = velocity;

        _externalGroundTimer = Mathf.Max(_externalGroundTimer, duration);
    }

    [Server]
    public void ServerSetControlMultiplier(float multiplier, float duration)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        TargetRpcSetControlMultiplier(coon, multiplier, duration);
    }

    [TargetRpc]
    public void TargetRpcSetControlMultiplier(NetworkConnection coon, float multiplier, float duration)
    {
        _controlMultiplier = Mathf.Clamp01(multiplier);
        _controlTimer = Mathf.Max(_controlTimer, duration);
    }

    [Server]
    private IEnumerator ClearStagger(float delay)
    {
        yield return new WaitForSeconds(delay);

        isStaggered = false;
    }

    public void OnStaggerChanged(bool oldValue, bool newValue)
    {
        if (_staggerIndicator != null)
        {
            _staggerIndicator.gameObject.SetActive(newValue);
            
            var rotateTool = _staggerIndicator.GetComponent<RotateAroundTool>();
            if (rotateTool != null) rotateTool.enabled = newValue;
        }
    }
    
    public void OnBlindedChanged(bool oldValue, bool newValue)
    {
        // Ativa/desativa o indicador de bosta (similar ao staggerIndicator)
        if (_bostaIndicator != null)
        {
            _bostaIndicator.gameObject.SetActive(newValue);
            
            var rotateTool = _bostaIndicator.GetComponent<RotateAroundTool>();
            if (rotateTool != null) rotateTool.enabled = newValue;
        }
        
        // Ativa/desativa o GameObject da bosta na cara
        if (_gameObjectBosta != null)
        {
            _gameObjectBosta.SetActive(newValue);
        }
        
        Debug.Log($"💩 [BLINDED] Indicador e bosta na cara: {(newValue ? "ATIVADO" : "DESATIVADO")}");
    }

    [Command]
    private void CmdSetStaggered(bool active)
    {
        isStaggered = active;
    }
    
    [Command]
    private void CmdSetBlinded(bool active)
    {
        isBlinded = active;
    }

    public void OnHitKill()
    {
        if (base.isOwned == false) return;
        OnContextualHit(DeathCause.Default, false);
    }
    public void OnHitSpectate()
    {
        if (base.isOwned == false) return;
        OnContextualHit(DeathCause.Default, true);
    }
    
    /// <summary>
    /// Método chamado pelo servidor para forçar a morte/espectador de um jogador.
    /// Diferente de OnHitSpectate que só funciona no cliente local.
    /// </summary>
    [Server]
    public void ServerForceSpectate(DeathCause cause = DeathCause.Default)
    {
        Debug.Log($"💀 [SERVER] Forçando espectador para {gameObject.name}");
        
        // Atualiza o estado no servidor
        isStaggered = false;
        
        // Envia RPC para o cliente dono do PlayerScript
        var conn = connectionToClient;
        if (conn != null)
        {
            TargetForceSpectate(conn, cause);
        }
        
        // Também envia RPC para todos verem os efeitos de morte
        RpcOnDeathWithCause(cause, true, transform.position, transform.rotation);
    }
    
    /// <summary>
    /// TargetRpc enviado apenas para o dono do PlayerScript para iniciar o modo espectador.
    /// </summary>
    [TargetRpc]
    private void TargetForceSpectate(NetworkConnection conn, DeathCause cause)
    {
        Debug.Log($"💀 [CLIENT] Recebido TargetForceSpectate - cause: {cause}");
        
        var entry = deathEffects != null ? deathEffects.Get(cause) : null;
        float hideDelay = deathEffects != null ? deathEffects.GetHideDelay(cause) : 0f;
        bool shouldHideModel = entry == null || entry.hideModelAfterDelay;
        if (entry != null && entry.hideModelAfterDelay)
            hideDelay = Mathf.Max(hideDelay, entry.hideModelDelay);
        
        _animator?.SetInteger(_DEATHCAUSE, (int)cause);
        
        // Inicia o processo de morte permanente (espectador)
        InternalDeath(true, hideDelay, shouldHideModel);
    }
    
    private void OnExtraFreezeChanged(bool oldVal, bool newVal)
    {
        
    }
    #region Menu
    private void EventOnCelularMenu()
    {
        if (base.isOwned == false) return;
        // Debounce para evitar toggles múltiplos quando há vários jogadores (ParrelSync/instâncias)
        if (Time.time < _nextMenuToggleTime) return;
        _nextMenuToggleTime = Time.time + 0.2f;
        
        // Se o painel de cores está aberto, fecha ele
        if (panel)
        {
            HUDSO.HideColorChangePanel();
            return;
        }
        
        // Se o painel de minigames está aberto, fecha ele  
        if (UILocked)
        {
            HUDSO.HideMinigameSelectionPanel();
            return;
        }
        
        // Se nenhum painel está aberto, alterna o estado do menu celular
        // _menuOpen = !_menuOpen;
        // celularInstance.SetActive(_menuOpen);
        
        if (_menuOpen)
        {
            HUDSO.HideMenuPanel();
        }
        else
        {
            HUDSO.ShowMenuPanel();
        }
    }

    private void OnShowMenuPanel()
    {
        _menuOpen = true;
        celularInstance.SetActive(true);
    }

    private void OnHideMenuPanel()
    {
        _menuOpen = false;
        celularInstance.SetActive(false);
    }

    
    #endregion
    #region Sensibilidade

    public void CmdChangeSensitivity(float normalized)
    {
        sensibilidade = Mathf.Lerp(0f, 25f, normalized);
        Debug.LogWarning($"🛠️ [SERVER] Sensibilidade ajustada para {sensibilidade}");
    }
    public void RequestSensitivityChange(float normalized)
    {
        if (!isOwned) return;
        CmdChangeSensitivity(normalized);
    }

    #endregion
    #region System Network

    // Spectator System

    [TargetRpc]
    private void RpcSpectate(float hideDelay, bool shouldHideModel)
    {
        Debug.Log("👁️ [SPECTATOR] Entering spectator mode");
        _isSpectating = true;
        
        // Notifica o HUD que entrou em modo espectador
        HUDSO?.SetSpectatorMode(true);
        
        // Notifica o manager para carregar overlay e atualizar estado
        SpectatorManager.Instance?.OnLocalSpectatorEnter(this);
        // Replica estado de espectador para os demais clientes (apenas booleano)
        var pd = GetComponent<PlayerData>();
        pd?.CmdSetSpectating(true);

        // Disable player input for movement
        if (_playerInput != null)
            _playerInput.enabled = false;

        // Hide player model (respeita delay configurado)
        if (shouldHideModel)
        {
            if (hideDelay > 0f)
                StartCoroutine(HideModelAfterDelay(hideDelay));
            else
                HidePlayerModel();
        }

        // Disable player controller to prevent collision/physics
        if (_controller != null)
            _controller.enabled = false;

        // Find and spectate first alive player
        if (_cam != null)
        {
            UpdateAlivePlayersList();
            if (_alivePlayersCache.Count > 0)
            {
                _currentSpectatorIndex = 0;
                CurrentSpectatedTarget = _alivePlayersCache[0];
                SetCameraTarget(CurrentSpectatedTarget.cameraTarget);
                Debug.Log($"👁️ [SPECTATOR] Now spectating: {CurrentSpectatedTarget.name}");
                // Notifica alvo atual para o overlay
                SpectatorManager.Instance?.OnLocalSpectatorTargetChangedInternal(CurrentSpectatedTarget);
                // Não replicamos quem está sendo observado (design atual)
            }
            else
            {
                Debug.LogWarning("👁️ [SPECTATOR] No alive players to spectate");
                CurrentSpectatedTarget = null;
                SpectatorManager.Instance?.OnLocalSpectatorTargetChangedInternal(null);
            }
        }
    }

    public void SetCameraTarget(Transform newTarget)
    {
        // Fallback seguro: se o alvo não tiver cameraTarget configurado,
        // usa o transform do player que está sendo observado (quando disponível)
        if (newTarget == null)
        {
            var fallback = CurrentSpectatedTarget != null ? CurrentSpectatedTarget.transform : transform;
            Debug.LogWarning("[SPECTATOR] Camera target nulo. Aplicando fallback para alvo observado.");
            cameraTarget = fallback;
            return;
        }
        cameraTarget = newTarget;
    }

    // Expor o HUD usado por este player para unificar painéis em runtime
    public HUDSO GetHUD() => HUDSO;

    private void UpdateAlivePlayersList()
    {
        _alivePlayersCache.Clear();
        PlayerScript[] allPlayers = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        
        foreach (var player in allPlayers)
        {
            if (player != this && player.State != PlayerState.Death && !player._isSpectating)
            {
                _alivePlayersCache.Add(player);
            }
        }
        
        Debug.Log($"👁️ [SPECTATOR] Found {_alivePlayersCache.Count} alive players");
    }

    private void CycleToNextSpectatorTarget()
    {
        if (_alivePlayersCache.Count == 0)
        {
            UpdateAlivePlayersList();
            if (_alivePlayersCache.Count == 0) return;
        }

        _currentSpectatorIndex = (_currentSpectatorIndex + 1) % _alivePlayersCache.Count;
        CurrentSpectatedTarget = _alivePlayersCache[_currentSpectatorIndex];
        SetCameraTarget(CurrentSpectatedTarget.cameraTarget);
        Debug.Log($"👁️ [SPECTATOR] Switched to: {CurrentSpectatedTarget.name}");
        SpectatorManager.Instance?.OnLocalSpectatorTargetChangedInternal(CurrentSpectatedTarget);
        // Não replicamos alvo observado
    }

    private void CycleToPreviousSpectatorTarget()
    {
        if (_alivePlayersCache.Count == 0)
        {
            UpdateAlivePlayersList();
            if (_alivePlayersCache.Count == 0) return;
        }

        _currentSpectatorIndex--;
        if (_currentSpectatorIndex < 0)
            _currentSpectatorIndex = _alivePlayersCache.Count - 1;

        CurrentSpectatedTarget = _alivePlayersCache[_currentSpectatorIndex];
        SetCameraTarget(CurrentSpectatedTarget.cameraTarget);
        Debug.Log($"👁️ [SPECTATOR] Switched to: {CurrentSpectatedTarget.name}");
        SpectatorManager.Instance?.OnLocalSpectatorTargetChangedInternal(CurrentSpectatedTarget);
        // Não replicamos alvo observado
    }

    private void HidePlayerModel()
    {
        CmdEventOnDeath();
    }

    private void ShowPlayerModel()
    {
        if (playerModelRoot != null)
        {
            playerModelRoot.SetActive(true);
            Debug.Log("✅ [RESPAWN] Player model shown");
        }
        else
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
            Debug.Log($"✅ [RESPAWN] Enabled {renderers.Length} renderers");
        }
    }


    [TargetRpc]
    private void RpcRespawn(Vector3 position)
    {
        if (_controller != null && isLocalPlayer)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }
        else
        {
            transform.position = position;
        }

        // Show player model again after respawn
        ShowPlayerModel();

        Debug.Log($"🎮 [CLIENT] Player {netId} respawned @ {position}");
    }

    // isso

    [TargetRpc]
    public void TargetRpcTeleport(NetworkConnection conn, Vector3 pos, Quaternion rot)
    {
        InternalTeleport(pos, rot);
    }


    void InternalTeleport(Vector3 pos, Quaternion rot)
    {
        _controller.enabled = false;
        transform.position = pos;
        //transform.rotation = rot;
        _smoothSyncMirror.setRotation(rot, true);
        
        if (isOwned)
        {
            _yaw = rot.eulerAngles.y;
            _pitch = Mathf.Clamp(_pitch, db.minMouseY, db.maxMouseX);
        }
        
        _smoothSyncMirror.teleportOwnedObjectFromOwner();
        _controller.enabled = true;

        InternalResetProperties();
    }

    public void InternalDeath(bool permaDeath, float hideDelay = 0f, bool shouldHideModel = true)
    {
        State = PlayerState.Death;
        
        if (permaDeath)
        {
            // Garante delay mínimo de 2 segundos para visualizar animação de morte
            float spectatorDelay = Mathf.Max(2f, hideDelay);
            Debug.Log($"💀 [DEATH] Morte permanente - aguardando {spectatorDelay}s antes de entrar em modo espectador");
            
            // Delay para entrar em modo espectador (permite ver animação)
            StartCoroutine(DelayedSpectatorMode(spectatorDelay, shouldHideModel));
        }
        else
        {
            _controller.enabled = false;
            InternalResetProperties();
            CmdDeath();
        }
    }
    
    private IEnumerator DelayedSpectatorMode(float delay, bool shouldHideModel)
    {
        yield return new WaitForSeconds(delay);
        CmdRequestSpectate(0f, shouldHideModel); // Já esperamos o delay, passa 0 para o RPC
    }

    [Command]
    private void CmdRequestSpectate(float hideDelay, bool shouldHideModel)
    {
        isStaggered = false;
        RpcSpectate(hideDelay, shouldHideModel);
    }

    public void OnContextualHit(DeathCause cause, bool perma)
    {
        if (!base.isOwned) return;

        var entry = deathEffects != null ? deathEffects.Get(cause) : null;
        float hideDelay = deathEffects != null ? deathEffects.GetHideDelay(cause) : 0f;
        bool shouldHideModel = entry == null || entry.hideModelAfterDelay;
        if (entry != null && entry.hideModelAfterDelay)
            hideDelay = Mathf.Max(hideDelay, entry.hideModelDelay);
        _suppressHideOnDeath = (entry != null && entry.hideModelAfterDelay);

        Debug.Log($"💀 [DEATH] Cause: {cause}, Perma: {perma}, SuppressHide: {_suppressHideOnDeath}");
        _animator?.SetInteger(_DEATHCAUSE, (int)cause);

        InternalDeath(perma, hideDelay, shouldHideModel);

        CmdDeathWithCause(cause, perma, transform.position, transform.rotation);

        _suppressHideOnDeath = false;
    }

    [Command]
    private void CmdDeathWithCause(DeathCause cause, bool perma, Vector3 pos, Quaternion rot)
    {
        isStaggered = false;
        RpcOnDeathWithCause(cause, perma, pos, rot);
    }

    [ClientRpc]
    private void RpcOnDeathWithCause(DeathCause cause, bool perma, Vector3 pos, Quaternion rot)
    {
        _animator?.SetInteger(_DEATHCAUSE, (int)cause);
        if (deathEffects != null)
        {
            var entry = deathEffects.Get(cause);
            if (entry != null)
            {

                if (entry.vfxPrefab != null)
                {
                    Vector3 spawnPos = transform.position;
                    Quaternion spawnRot = rot;
                    var vfx = GameObject.Instantiate(entry.vfxPrefab, spawnPos, spawnRot);
                    if (entry.attachToPlayer && vfx != null)
                    {
                        vfx.transform.SetParent(transform, worldPositionStays: true);
                    }
                    if (entry.vfxLifetime > 0f)
                        GameObject.Destroy(vfx, entry.vfxLifetime);
                }

                if (entry.sfx != null)
                {
                    AudioSource.PlayClipAtPoint(entry.sfx, transform.position, entry.sfxVolume);
                }

                float delay = deathEffects.GetHideDelay(cause);
                if (entry.hideModelAfterDelay)
                {
                    delay = Mathf.Max(delay, entry.hideModelDelay);
                    // No owner: aplica normalmente; owner perma usa RpcSpectate (para respeitar delay)
                    bool handledBySpectatorFlow = perma && base.isOwned;
                    if (!handledBySpectatorFlow)
                        StartCoroutine(HideModelAfterDelay(delay));
                }
                return;
            }
        }
    }

    private IEnumerator HideModelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePlayerModel();
    }

    void InternalResetProperties()
    {
        Status = PlayerStatus.Default;

        _move = Vector3.zero;
        _inertia = Vector3.zero;

        _staggerTimer = 0;
        _pushCooldown = 0;
        _rollTimer = 0;
        _rollCooldown = 0;
        _blindTimer = 0;
        _poopSlowTimer = 0;
        _throwCooldown = 0;
        
        // Garante que o estado blinded seja desativado ao resetar
        if (isLocalPlayer && isOwned)
        {
            CmdSetBlinded(false);
        }
    }
    [Command]
    void CmdDeath()
    {
        Debug.LogWarning("⚠️ [CMD] Death");
        this.EventOnDeathServerSide?.Invoke();
    }

    [Command]
    void CmdEventOnDeath()
    {
        isStaggered = false;
        RpcOnDeath();
    }

    [ClientRpc]
    public void RpcOnDeath()
    {
        Debug.Log("📡 [RPC] OnDeath()");
        this.EventOnDeath?.Invoke();

    }

    [ClientRpc]
    public void RpcOnRespawn()
    {
        Debug.Log("📡 [RPC] OnRespawn()");
        this.EventOnRespawn?.Invoke();

        if (base.isOwned == false) return;

        // Exit spectator mode if in spectator
        if (_isSpectating)
        {
            ExitSpectatorMode();
        }

        // Show player model again
        ShowPlayerModel();

        // Re-enable controller
        if (_controller != null)
            _controller.enabled = true;

        // Re-enable input
        if (_playerInput != null)
            _playerInput.enabled = true;

        // Reset camera target to self
        cameraTarget = transform;

        InternalResetProperties();
        State = PlayerState.Default;
    }

    private void ExitSpectatorMode()
    {
        Debug.Log("✅ [SPECTATOR] Exiting spectator mode");
        _isSpectating = false;
        _alivePlayersCache.Clear();
        _currentSpectatorIndex = 0;
        CurrentSpectatedTarget = null;
        
        // Notifica o HUD que saiu do modo espectador
        HUDSO?.SetSpectatorMode(false);
        
        // Notifica o manager para descarregar overlay
        SpectatorManager.Instance?.OnLocalSpectatorExit(this);
        // Replica saída do espectador
        var pd = GetComponent<PlayerData>();
        if (pd != null)
        {
            pd.CmdSetSpectating(false);
        }
    }

    // Métodos públicos para a UI do overlay navegar entre alvos
    public void SpectateNextTarget()
    {
        if (!_isSpectating) return;
        CycleToNextSpectatorTarget();
    }

    public void SpectatePreviousTarget()
    {
        if (!_isSpectating) return;
        CycleToPreviousSpectatorTarget();
    }

    #endregion
    
    #region Customization

    public void ApplyPlayerCustomization()
    {
        var applier = GetComponentInChildren<CustomizationApplier>();
        if (applier == null)
        {
            Debug.LogWarning("⚠️ [PlayerScript] CustomizationApplier not found. Make sure it's added to the player prefab");
            return;
        }
        
        if (isLocalPlayer)
        {
            if (CustomizationManager.Instance == null)
            {
                Debug.LogWarning("⚠️ [PlayerScript] CustomizationManager not initialized");
                return;
            }
            
            var customization = CustomizationManager.Instance.GetCurrentCustomization();
            if (customization != null)
            {
                var playerData = GetComponent<PlayerData>();
                if (playerData != null)
                {
                    playerData.SendCustomizationToServer();
                    Debug.Log($"📤 [PlayerScript] Sent customization to server via PlayerData: {customization}");
                }
                
                applier.ApplyCustomization(customization);
                Debug.Log("✅ [PlayerScript] Customization applied to local player");
            }
        }
        else
        {
            var playerData = GetComponent<PlayerData>();
            if (playerData != null)
            {
                var customData = new PlayerCustomizationData("");
                customData.hatIndex = playerData.hatIndex;
                customData.glassesIndex = playerData.glassesIndex;
                customData.shirtIndex = playerData.shirtIndex;
                
                applier.ApplyCustomization(customData);
                Debug.Log($"✅ [PlayerScript] Customization applied from PlayerData SyncVars: Hat={playerData.hatIndex}, Glasses={playerData.glassesIndex}, Shirt={playerData.shirtIndex}");
            }
        }
    }
    

    public void ApplyRemoteCustomization(int hatIndex, int glassesIndex, int shirtIndex)
    {
        if (isLocalPlayer) return; 
        
        var applier = GetComponentInChildren<CustomizationApplier>();
        if (applier != null)
        {
            var customData = new PlayerCustomizationData("");
            customData.hatIndex = hatIndex;
            customData.glassesIndex = glassesIndex;
            customData.shirtIndex = shirtIndex;
            
            applier.ApplyCustomization(customData);
            Debug.Log($"✅ [PlayerScript] Remote customization applied: Hat={hatIndex}, Glasses={glassesIndex}, Shirt={shirtIndex}");
        }
    }

    #endregion
}
