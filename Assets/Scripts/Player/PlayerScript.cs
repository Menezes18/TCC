using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
        inertia += input * Time.deltaTime;
        
        inertia = Vector3.ClampMagnitude(inertia, _inertiaSpeed);

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

    public void RespawnAt(Vector3 position)
    {
        if (!isServer) return;

        RpcRespawn(position);
    }

    [TargetRpc]
    private void RpcRespawn(Vector3 position)
    {
        if (_characterController != null)
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
            _characterController.Move(force);
            
            _move.y = force.magnitude * 0.5f;
            
            State = PlayerStates.Air;
            _inertiaSpeed = force.magnitude;
        }
    }
    protected override void OnStateChanged(PlayerStates oldVal, PlayerStates newVal){
        base.OnStateChanged(oldVal, newVal);
    }
}
