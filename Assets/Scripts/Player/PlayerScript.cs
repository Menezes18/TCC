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

    public Vector3 rot => new Vector3(0, Camera.main.transform.rotation.eulerAngles.y, 0);

    [SerializeField] private TMP_Text _role;
    private Vector2 _input;
    private Vector3 _move;
    private bool _isGrounded;
    private bool _ignoreGroundedOnThisFrame;
    private float _inertiaSpeed;
    [SyncVar(hook = nameof(OnAliasUpdated))]  public string Alias; 
    [SyncVar]
    public string sessionId = "";
    private void Start(){
        
        if(base.isOwned == false) return;
        
        _playerSO.EventOnCustomMove += EventOnCustomMove;
        _playerSO.EventOnJump += EventOnJump;
    }
    
    void OnAliasUpdated(string oldVal, string newVal){
        _role.text = newVal;
    }
    
    [Command]
    private void Command(string str){
      Alias = str;
      
    }
    private void Update(){
        
        if(base.isOwned == false) return;
        
        BehaviorAir();
        BehaviorDefault();

        _move += Vector3.up * db.gravity * Time.deltaTime;
        
        _characterController.Move(_move * Time.deltaTime);
        
        if (_characterController.isGrounded == true)
            _move.y = db.gravityGrounded;
        
        
        if(_input.magnitude == 0) return;
        
        Command(_input.ToString());
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
            
            State = PlayerStates.Default;
        }
    }
    private void BehaviorDefault(){
        if(State != PlayerStates.Default) return;
        float vertical = _move.y;
        
        _move = new Vector3(_input.x, 0, _input.y);
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed;
        _move.y = vertical;
    }

    private void EventOnJump(InputAction.CallbackContext obj){
        if (_characterController.isGrounded == false) return;
        
        _inertiaSpeed = new Vector3(_move.x, 0, _move.z).magnitude;
        _inertiaSpeed = Mathf.Clamp(_inertiaSpeed, db.playerSpeed, db.maxAirSpeed);
        State = PlayerStates.Air;
        _ignoreGroundedOnThisFrame = true;
        
        _move.y = db.jumpHeight;
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
    [ClientRpc]
    public void RpcTeleport(Vector3 position, Quaternion rotation)
    {
        // Desativar temporariamente a física para evitar problemas de sincronização
        Rigidbody rb = GetComponent<Rigidbody>();
        bool wasKinematic = false;
        
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }
        
        // Definir posição e rotação
        transform.position = position;
        transform.rotation = rotation;
        
        // Restaurar a física
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }
        
        Debug.Log($"Player {name} teleported to {position}");
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
    

    private IEnumerator ReturnToDefaultStateAfterPush()
    {
        yield return new WaitForSeconds(0.5f);
        
        
        if (State == PlayerStates.BeingPushed)
        {
             
            if (!_characterController.isGrounded)
                State = PlayerStates.Air;
            else
                State = PlayerStates.Default;
        }
    }
    protected override void OnStateChanged(PlayerStates oldVal, PlayerStates newVal){
        base.OnStateChanged(oldVal, newVal);
    }
    
// Método RPC para definir a posição do jogador em todos os clientes
    [ClientRpc]
    public void RpcSetPosition(Vector3 position, Quaternion rotation)
    {
        // Desativar temporariamente a física para evitar problemas de sincronização
        Rigidbody rb = GetComponent<Rigidbody>();
        bool hadRigidbody = false;
        
        if (rb != null)
        {
            hadRigidbody = rb.isKinematic;
            rb.isKinematic = true;
        }
        
        // Definir posição e rotação
        transform.position = position;
        transform.rotation = rotation;
        
        // Restaurar a física
        if (rb != null && !hadRigidbody)
        {
            rb.isKinematic = false;
        }
        
        Debug.Log($"Player {name} position set to {position}");
    }
    
    // Chamado quando o jogador é inicializado no servidor
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Registrar no PlayerManager
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RegisterPlayer(this);
            Debug.Log($"Player {name} registered with PlayerManager");
        }
        else
        {
            Debug.LogError("PlayerManager instance not found!");
        }
    }
    
    // Chamado quando o jogador é inicializado no cliente
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Se for o cliente local, registrar no PlayerManager
        if (isLocalPlayer && PlayerManager.Instance != null)
        {
            Debug.Log($"Local player {name} started");
        }
    }
    
    // Chamado quando o jogador é desconectado no servidor
    public override void OnStopServer()
    {
        base.OnStopServer();
        
        // Desregistrar do PlayerManager
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterPlayer(this);
        }
    }
}