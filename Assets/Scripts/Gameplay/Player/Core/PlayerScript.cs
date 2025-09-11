using System;
using System.Collections;
using Mirror;
using Smooth;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic; // for future
// using for DI helper (global namespace static so technically not required if also global)
// Added for damage effects
// damage effects in global namespace

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
public partial class PlayerScript : NetworkBehaviour, IDamageable, IHitKillable
{
    [SerializeField] Database db;
    [SerializeField] PlayerControlsSO PlayerControlsSO;
    [SerializeField] HUDSO HUDSO; // legado
    private IHudEvents _hudEvents; // fase 4 item 13
    [SerializeField] SmoothSyncMirror _smoothSyncMirror;

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
                _animator.SetTrigger(AnimatorParams.Throw);
                _networkAnimator.SetTrigger(AnimatorParams.Throw);
            }

            if (value != PlayerStatus.Pushing) return;

            // NetworkAnimator não replica trigger
            // Então tem que passar sempre nos 2
            // animator --> trigger
            // networkAnimator --> trigger
            _animator.SetTrigger(AnimatorParams.Push);
            _networkAnimator.SetTrigger(AnimatorParams.Push);

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

    private float _staggerTimer;
    private float _rollTimer; // duração ativa do roll
    private float _blindTimer; // status blind
    private PlayerCooldowns _cooldowns = new PlayerCooldowns();

    public float PushCooldownNormalized => _cooldowns.GetNormalized(PlayerCooldownType.Push, db.playerPushCooldownTimer);
    public float ThrowCooldownNormalized => _cooldowns.GetNormalized(PlayerCooldownType.Throw, db.playerThrowCooldown);
    private float BlindTimer {
        get => _blindTimer;
        set {
            if (_blindTimer == value) return;
            _blindTimer = value;
        }
    }


    private PlayerInput _playerInput;
    private PlayerContext _context;
    private IPlayerAbility _pushAbility;
    private ThrowAbility _throwAbility;
    private PlayerCameraController _cameraController;
    // Fase 3 Item 9: damage registry reference
    private DamageRegistry _damageRegistry;

    [Header("Input Schemes")]
    [SerializeField] private string keyboardMouseScheme = "Keyboard&Mouse";
    [SerializeField] private string gamepadScheme = "Gamepad";

    public bool IsAirborne => State == PlayerState.Ascend || State == PlayerState.Descend;
    [SyncVar(hook = nameof(OnExtraFreezeChanged))]
    public bool _extraFreeze;
    [SerializeField] private MatchManager _matchManager; // soft ref (Item 14)
    public bool isFrozen
    {
        get => (_matchManager ?? MatchManager.singleton)?.Freeze == true || _extraFreeze;
        [Server]
        set => _extraFreeze = value;
    }


    public Transform cameraTarget;

    [SyncVar(hook = nameof(OnStaggerChanged))]
    private bool isStaggered;

    private bool _menuOpen = false;
    public bool panel = false;
    // Bloqueia movimento/olhar enquanto o chat estiver aberto
    [SerializeField] private bool _chatOpen = false;

    [SerializeField] private float sensibilidade = 1;
    [SyncVar(hook = nameof(OnCarryingChanged))] private bool _isCarrying;
    [SerializeField, Range(0.3f, 1f)] private float carryingSpeedMultiplier = 0.8f;
    public bool IsCarrying => _isCarrying;

    // UI

    [Header("Prefabs")]
    [SerializeField] private GameObject canvasCelularPrefab;
    private GameObject celularInstance;
    public MainMenu mainMenu;
    [SerializeField] private GameObject cooldownUIPrefab;
    GameObject cooldownUIInstance;

    // Event
    public UnityEvent EventOnDeath;
    public UnityEvent EventOnDeathServerSide;
    public UnityEvent EventOnRespawn;
    public UnityEvent EventOnJump;
    public UnityEvent EventOnPush;


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

        //
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (_cam == null)
        {
            if (Camera.main != null) _cam = Camera.main.transform;
            else Debug.LogWarning("[PlayerScript] No main camera found to assign as _cam.");
        }
        cameraTarget = transform;

        _playerInput = GetComponent<PlayerInput>();
        // Não desabilitar o NetworkAnimator no local player. Deixe sempre habilitado para sincronizar parâmetros.

        ConfigureControlScheme(initial:true);

        if (PlayerPrefs.HasKey("MouseSensitivity"))
            sensibilidade = PlayerPrefs.GetFloat("MouseSensitivity");

        // Soft resolve de dependências opcionais
        _matchManager = SingletonFallback.Resolve(_matchManager, () => MatchManager.singleton, this, nameof(_matchManager));
        _context = new PlayerContext(this, _cooldowns, db, _animator, _networkAnimator, _cam);
        _hudEvents = new HudSoAdapter(HUDSO); // simples: adaptador local
        _pushAbility = new PushAbility();
        _throwAbility = new ThrowAbility();
        _cameraController = new PlayerCameraController(_cam, db, transform);
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
        if (cooldownUIPrefab != null)
        {
            cooldownUIInstance = Instantiate(cooldownUIPrefab);
            var ui = cooldownUIInstance.GetComponent<CooldownUI>();
            if (ui != null)
                ui.Init(this);
        }
    }
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        PlayerControlsSO.OnMenu -= EventOnCelularMenu;
        if (cooldownUIInstance != null)
            Destroy(cooldownUIInstance);


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
        PlayerControlsSO.OnMove -= PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook -= PlayerControlsSO_OnLook;
        PlayerControlsSO.OnJump -= PlayerControlsSO_OnJump;
        PlayerControlsSO.OnPush -= PlayerControlsSO_OnPush;
        PlayerControlsSO.OnRoll -= PlayerControlsSO_OnRoll;
        PlayerControlsSO.OnThrow -= PlayerControlsSO_OnThrow;
        PlayerControlsSO.OnThrowCancel -= PlayerControlsSO_OnThrowCancel;

        //UI
        PlayerControlsSO.OnMenu -= EventOnCelularMenu;
        // PlayerControlsSO.OnCursor -= PlayerControlsSO_OnCursor;
        if (cooldownUIInstance != null)
            Destroy(cooldownUIInstance);
    }

    private void Update()
    {
        if (!this.isOwned) return;
        if (!isLocalPlayer) return;
        //
        _cooldowns.Tick(Time.deltaTime);

        if (_staggerTimer > 0) _staggerTimer -= Time.deltaTime;

        if (_rollTimer > 0) _rollTimer -= Time.deltaTime;

        // roll cooldown agora controla via wrapper quando sair do estado

        if (_blindTimer > 0) _blindTimer -= Time.deltaTime;
        
        if (Keyboard.current.pKey.wasPressedThisFrame ) // input
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
        if (Keyboard.current.oKey.wasPressedThisFrame )
        {
            HUDSO.ShowColorChangePanel(); 
        }

        // ===== Spectate Cycling (Phase1 small improvement) =====
        if (State == PlayerState.Death && !_menuOpen)
        {
            HandleSpectateInput();
        }
        float blindWeight = CustomMath.ConvertRange(_blindTimer, db.playerBlindDuration, 0);
        float blindRange = db.playerBlindCurve.Evaluate(blindWeight);
        // via ScriptableObject ainda (HUDSO) + podemos futuramente mover para evento dedicado
        HUDSO.SetBlindAlpha(blindRange); // mantido para não quebrar listeners existentes

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

        if (Status == PlayerStatus.Throw && _cooldowns.IsReady(PlayerCooldownType.Throw))
            Status = PlayerStatus.Default;
        if (Status == PlayerStatus.Blinded && _cooldowns.IsReady(PlayerCooldownType.Throw))
            Status = PlayerStatus.Default;

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

        if (_controller.isGrounded) {
            _move.y = db.gravityGrounded;
        }

        transform.rotation = Quaternion.Euler(rot);

    }
    private void LateUpdate()
    {
        if (!this.isOwned) return;

    _cameraController?.Tick(_pitch, _yaw);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("KillPlane"))
            InternalDeath(false);
    }
    
    [Server] public void ServerSetCarrying(bool value) { _isCarrying = value; }
    private void OnCarryingChanged(bool oldVal, bool newVal)
    {

    }
    private float GetSpeedMultiplier() => _isCarrying ? carryingSpeedMultiplier : 1f;


    //
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


    }
    

    //
    

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
    // Guard clause refactor lives in partial Combat file (Phase 1 PoC)
    // (state change handler in Partial/PlayerScript.State.cs)
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
        var instancer = PrefabInstancer.singleton; // futuro: serializar
        if (instancer == null)
        {
            Debug.LogWarning("[PlayerScript] PrefabInstancer indisponível (singleton null).");
            return;
        }
        instancer.CmdSpawnProjectile(origin.transform.position, direction, this.netIdentity);

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
    // (network damage handlers in Partial/PlayerScript.Network.cs)

    //
    public void PassEvent(PlayerVfx fxState) {
        if (fxState == PlayerVfx.Ascendfx) OnEventPass(this.EventOnJump);
        if (fxState == PlayerVfx.Pushingfx) OnEventPass(this.EventOnPush);
    }
    private void OnEventPass(UnityEvent unityEventAct)
    {
        unityEventAct?.Invoke();
    }
    

    public void OnHitKill()
    {
        if (base.isOwned == false) return;

        InternalDeath(false);
    }
    public void OnHitSpectate()
    {
        if (base.isOwned == false) return;
        InternalDeath(true);

    }
    private void OnExtraFreezeChanged(bool oldVal, bool newVal)
    {
        
    }
    #region Menu
    private void EventOnCelularMenu()
    {
        if (base.isOwned == false) return;
        _menuOpen = !_menuOpen;
        
        celularInstance.SetActive(_menuOpen);
        
        if (panel) {
            HUDSO.HideColorChangePanel();
            return;
        }
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

    // mudar isso 

    [TargetRpc]
    private void RpcSpectate()
    {
        if (_playerInput == null) return;
        _playerInput.enabled = false;

        if (_cam == null) return;
        Transform newTarget = FindSpectatorTarget();
        if (newTarget == null) return;
        SetCameraTarget(newTarget);
    }

    public void SetCameraTarget(Transform newTarget)
    {
        cameraTarget = newTarget;
    _cameraController?.SetTarget(newTarget);
    }

    private Transform FindSpectatorTarget()
    {
    PlayerScript[] players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player != this && player.State != PlayerState.Death)
            {
                return player.transform;
            }
        }
        return null;
    }

    // ===== Spectate Support =====
    private int _spectateIndex = -1;
    private PlayerScript[] _spectateCache = Array.Empty<PlayerScript>();
    private float _spectateRefreshTimer;
    private const float SPECTATE_REFRESH_INTERVAL = 1.5f;

    private void RefreshSpectateCache()
    {
        _spectateCache = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None)
            .Where(p => p != this && p.State != PlayerState.Death)
            .ToArray();
        if (_spectateCache.Length == 0)
        {
            _spectateIndex = -1;
            return;
        }
        if (_spectateIndex < 0 || _spectateIndex >= _spectateCache.Length)
            _spectateIndex = 0;
        SetCameraTarget(_spectateCache[_spectateIndex].transform);
    }

    private void HandleSpectateInput()
    {
        _spectateRefreshTimer -= Time.deltaTime;
        if (_spectateRefreshTimer <= 0f)
        {
            _spectateRefreshTimer = SPECTATE_REFRESH_INTERVAL;
            RefreshSpectateCache();
        }
        if (_spectateCache.Length == 0) return;

        bool next = Keyboard.current.eKey != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool prev = Keyboard.current.qKey != null && Keyboard.current.qKey.wasPressedThisFrame;
        if (!next && !prev) return;
        if (next)
            _spectateIndex = (_spectateIndex + 1) % _spectateCache.Length;
        else if (prev)
            _spectateIndex = (_spectateIndex - 1 + _spectateCache.Length) % _spectateCache.Length;
        SetCameraTarget(_spectateCache[_spectateIndex].transform);
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
        transform.rotation = rot;
        _smoothSyncMirror.teleportOwnedObjectFromOwner();
        _controller.enabled = true;

        InternalResetProperties();
    }

    public void InternalDeath(bool permaDeath)
    {
        _controller.enabled = false;
        if (permaDeath)
        {
            RpcSpectate();
        }
        else
        {
            InternalResetProperties();
            CmdDeath();
        }

        State = PlayerState.Death;
    }

    void InternalResetProperties()
    {
        Status = PlayerStatus.Default;

        _move = Vector3.zero;
        _inertia = Vector3.zero;

        _staggerTimer = 0;
        _rollTimer = 0;
        _blindTimer = 0;
        _cooldowns.ResetAll();
    }
    [Command]
    void CmdDeath()
    {
        Debug.LogWarning("⚠️ [CMD] Death");
        this.EventOnDeathServerSide?.Invoke();
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

        InternalResetProperties();
        State = PlayerState.Default;

    }

    public void ApplyBlind(float duration)
    {
        _blindTimer = duration;
    }
    public void ApplyPush(Vector3 impulse, float verticalBoost, float staggerStun)
    {
        _inertia = impulse;
        InertiaCap = impulse.magnitude;
        _move.y = verticalBoost;
        _staggerTimer = staggerStun;
    }

    #endregion
}
