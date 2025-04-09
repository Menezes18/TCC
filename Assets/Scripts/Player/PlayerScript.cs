using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerScript : PlayerScriptBase
{
    [Header("Configuração do Player")]
    [SerializeField]  Database db;
    [SerializeField]  PlayerControlSO _playerSO;
    [SerializeField]  CharacterController _characterController;
    [SerializeField] private GameObject _panel;

    public PlayerInputScript _playerInputScript;
    public Vector3 rot {
        get {
            PlayerCamera cam = GetComponentInChildren<PlayerCamera>();
            if (cam != null)
            {
                return new Vector3(0, cam.CurrentCameraYRotation, 0);
            }
            return new Vector3(0, Camera.main.transform.rotation.eulerAngles.y, 0);
        }
    }

    public Animator _animator;
    public Camera cameraJogador;
    public Vector2 _input;
    private Vector3 _move;
    private bool _isGrounded;
    private bool _ignoreGroundedOnThisFrame;
    private float _inertiaSpeed;
    [SerializeField ]private PlayerInput _playerInput;
    [SyncVar(hook = nameof(OnAliasUpdated))]  public string Alias; 
    [SyncVar]
    public string sessionId = "";
    private PlayerShootSystem _playerShootSystem;
    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }
    }

    private void Start(){
        
        if(base.isOwned == false) return;
        
        _playerSO.EventOnCustomMove += EventOnCustomMove;
        _playerSO.EventOnJump += EventOnJump;
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (isOwned)
        {
            _playerSO.EventOnCustomMove += EventOnCustomMove;
            _playerSO.EventOnJump += EventOnJump;
        }
    }

    public bool teste = false;
    protected override void OnValidate()
    {
        if (!teste && _playerInput != null){
            //SetInputActionEnabled(new string[] { "Player"}, true);
            //SetInputEnabled(new string[] { "Player"}, true, true);
            _animator.SetBool("PushPFrente", true);
        }
        else if (_playerInput != null){
            _animator.SetBool("PushPFrente", false);
            //SetInputActionEnabled(new string[] { "Player"}, false);
            //SetInputEnabled(new string[] { "Player"}, false, true);
        }
    }
    private void OnDisable()
    {
        if (isOwned)
        {
            _playerSO.EventOnCustomMove -= EventOnCustomMove;
            _playerSO.EventOnJump -= EventOnJump;
        }
    }
        
    void OnAliasUpdated(string oldVal, string newVal){
    }

    [Command]
    private void Command(string str){
        Alias = str;
      
    }
    private void Update(){

        if(base.isOwned == false) return;
        
        BehaviorAir();
        BehaviorIdle();
        BehaviorDefault();

        _move += Vector3.up * db.gravity * Time.deltaTime;
        
        _characterController.Move(_move * Time.deltaTime);
        
        if (_characterController.isGrounded == true)
            _move.y = db.gravityGrounded;
        if (!IsInSpecialState()){
            
            if (_input.magnitude > 0.01f)
            {
                if (State != PlayerStates.Moving)
                    State = PlayerStates.Moving;
            }
            else
            {
                if (State != PlayerStates.Idle)
                    State = PlayerStates.Idle;
            }
        }
        
        if(_input.magnitude == 0) return;
        
        Command(_input.ToString());
    }
    private void BehaviorIdle()
    {
        if (State != PlayerStates.Idle) return;
    
        float vertical = _move.y;
        _move = Vector3.zero;
        _move.y = vertical;
    }

    private void BehaviorAir(){
        if(State != PlayerStates.Air) return; // Enter condition check
        
        Vector3 input = new Vector3(_input.x, 0, _input.y);
        input = Quaternion.Euler(rot) * input;
        input *= db.airSpeed;

        float vertical = _move.y;
        
        
        Vector3 inertia = _move;
        inertia.y = 0;
       // inertia += input * Time.deltaTime;
       //inertia = Vector3.ClampMagnitude(inertia, _inertiaSpeed);
        inertia = Vector3.Lerp(inertia, input * db.airSpeed, Time.deltaTime * 5f);

    // Permite que o player controle melhor sua direção no ar
        inertia = Vector3.ClampMagnitude(inertia, db.maxAirSpeed * 0.8f); 

        _move = inertia;
        _move.y = vertical;
        
        if (_ignoreGroundedOnThisFrame == true){
            _ignoreGroundedOnThisFrame = false;
            return;
        }
        
        if (_characterController.isGrounded == true){ // Exit condition  
            
            State = PlayerStates.Moving;
        }
    }
    private void BehaviorDefault(){
        if(State != PlayerStates.Moving) return;
        float vertical = _move.y;
        
        _move = new Vector3(_input.x, 0, _input.y);
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed;
        _move.y = vertical;
        
        _animator.SetFloat("MoveX", _playerInputScript.GetInput().x);
        _animator.SetFloat("MoveY", _playerInputScript.GetInput().y);
    }

    private void EventOnJump(InputAction.CallbackContext obj)
    {
        // Verifica se o CharacterController ainda existe
        if (_characterController == null || !_characterController.isGrounded)
            return;
        
        

        // Lógica para o pulo
        _inertiaSpeed = new Vector3(_move.x, 0, _move.z).magnitude;
        _inertiaSpeed = Mathf.Clamp(_inertiaSpeed, db.playerSpeed, db.maxAirSpeed);
        State = PlayerStates.Jump;
        _ignoreGroundedOnThisFrame = true;
        _move.y = db.jumpHeight;
        StartCoroutine(JumpToAirCoroutine());
    }
    private IEnumerator JumpToAirCoroutine()
    {
        yield return new WaitForSeconds(0.1f); 
        if (State == PlayerStates.Jump)
            State = PlayerStates.Air;
    }
    private void EventOnCustomMove(Vector2 obj){
        _input = obj;
    }
    [Command]
    public void RespawnAt(Vector3 position)
    {
        if (!isServer) return;

        RpcRespawn(position);
    }

    [TargetRpc]
    private void RpcRespawn(Vector3 position)
    {
        if (_characterController != null && isLocalPlayer)
        {
            _characterController.enabled = false; 
            transform.position = position;
            _characterController.enabled = true; 
        }
        else
        {
            transform.position = position;
        }

        Debug.Log($"[CLIENT] Player {netId} respawned at {position}");
    }

    [Command]
    public void Die()
    {
        State = PlayerStates.Dead;
        _characterController.enabled = false; 
        RpcSpectate();
    }

    [TargetRpc]
    private void RpcSpectate()
    {
        if (_playerInput != null)
        {
            _playerInput.enabled = false;
        }

        PlayerCamera cam = GetComponentInChildren<PlayerCamera>();
        if (cam != null)
        {
            Transform newTarget = FindSpectatorTarget();
            if (newTarget != null)
            {
                cam.SwitchTarget(newTarget);
            }
            else
            {
                Debug.LogWarning("Nenhum target de espectador encontrado!");
            }
        }
    }

    private Transform FindSpectatorTarget()
    {
        PlayerScript[] players = FindObjectsOfType<PlayerScript>();
        foreach (var player in players)
        {
            if (player != this && player.State != PlayerStates.Dead)
            {
                return player.transform;
            }
        }
        return null;
    }

    [ClientRpc]
    public void ApplyPush(Vector3 force)
    {
        if (_characterController != null)
        {
            
            _characterController.Move(force * 0.5f); 
        
            
            _move = new Vector3(force.x * 1.2f, 0, force.z * 1.2f); 
        
            // "levantar" o personagem 
            _move.y = force.magnitude * 0.7f; 
        
            
            _inertiaSpeed = Mathf.Max(force.magnitude, db.maxAirSpeed);
        
           
            State = PlayerStates.BeingPushed;
        }
    
        
        StartCoroutine(ReturnToDefaultStateAfterPush());
    }
    
    [TargetRpc]
    public void TargetOnHitByShot(NetworkConnectionToClient target)
    {
    
        State = PlayerStates.BeingShot;
    
        Debug.Log($"Player {netId} foi atingido por um tiro!");
    
        _panel.GetComponent<CanvasGroup>().alpha = 1;
    
        StartCoroutine(ReturnToDefaultStateAfterShot());
    }

    private IEnumerator ReturnToDefaultStateAfterShot()
    {
        yield return new WaitForSeconds(0.5f);
        _panel.GetComponent<CanvasGroup>().alpha = 0;
        if (State == PlayerStates.BeingShot)
        {
            if (!_characterController.isGrounded)
                State = PlayerStates.Air;
            else
                State = PlayerStates.Moving;
        }
    }

    private IEnumerator ReturnToDefaultStateAfterPush()
    {
        yield return new WaitForSeconds(0.5f);
        
        
        if (State == PlayerStates.BeingPushed)
        {
             
            if (!_characterController.isGrounded)
                State = PlayerStates.Air;
            else
                State = PlayerStates.Moving;
        }
    }
    public void SetInputEnabled(string[] inputNames, bool enabled, bool useActionMap = false)
    {
        foreach (string name in inputNames)
        {
            if (useActionMap)
            {
                InputActionMap map = _playerInput.actions.FindActionMap(name);
                if (map != null)
                {
                    if (enabled)
                        map.Enable();
                    else
                        map.Disable();
                }
                else
                {
                    Debug.LogWarning($"ActionMap '{name}' não encontrado.");
                }
            }
            else
            {
                InputAction action = _playerInput.actions.FindAction(name);
                if (action != null)
                {
                    if (enabled)
                        action.Enable();
                    else
                        action.Disable();
                }
                else
                {
                    Debug.LogWarning($"Action '{name}' não encontrada.");
                }
            }
        }
    }
    private bool IsInSpecialState()
    {
        return State == PlayerStates.Air ||
               State == PlayerStates.Jump ||
               State == PlayerStates.BeingShot ||
               State == PlayerStates.BeingPushed;
    }

    protected override void OnStateChanged(PlayerStates oldVal, PlayerStates newVal){
        base.OnStateChanged(oldVal, newVal);
    }
}
