using Mirror;
using UnityEngine;

public enum PlayerState{
    Default,
    Ascend,
    Descend,
}

public enum PlayerStatus{
    
}
public class PlayerScript : NetworkBehaviour{
    
    
    [SerializeField] PlayerControlsSO PlayerControlsSO;
    [SerializeField] CharacterController _controller;
    [SerializeField] Database db;

    [SerializeField] PlayerState _state;
    PlayerState State{
        get {return _state;}
        set{
            if(_state == value){return;}
            OnStateChanged(_state, value);
            _state = value;
        }
    }

    [SerializeField] private Transform _cam;

    public Vector3 rot => new Vector3(0, _cam.transform.rotation.eulerAngles.y, 0);

    private Vector3 _input;
    private Vector3 _raw;
    
    private Vector3 _move;
    private Vector3 _inertia;

    private float _inertiaCap;

    private float InertiaCap{
        get {return _inertiaCap;}
        set{
            _inertiaCap = Mathf.Clamp(value, db.playerSpeed, db.playerMaxAirSpeed);
        }
    }
    
    private float _mouseX, _mouseY;
    private bool _ignoreGroundedNextFrame;
    
    private void Start()
    {
        PlayerControlsSO.OnMove += PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook += PlayerControlsSO_OnLook;
        PlayerControlsSO.OnJump += PlayerControlsSO_OnJump;
        
        _cam = Camera.main.transform;
    }
    private void Update()
    {
        AerialDetection();
        AerialBehaviour();
        DefaultBehaviour();
        
        
        _move = _move + Vector3.up * db.gravity * Time.deltaTime;
        
        _controller.Move(_move * Time.deltaTime);

        if (_controller.isGrounded){
            _move.y = db.gravityGrounded;
        }
        
        transform.rotation = Quaternion.Euler(rot);

    }
    private void LateUpdate()
    {
        Vector3 newRot = new Vector3(_mouseY, _mouseX, 0);
        _cam.transform.rotation = Quaternion.Euler(newRot);
        _cam.transform.position = transform.position + _cam.rotation * db.orbitalOffset;

    }
    //
    private void AerialDetection()
    {
        
        if (_move.y > 0)
            State = PlayerState.Ascend;
        else if (_move.y < db.gravityGrounded)
            State = PlayerState.Descend;

        if (_ignoreGroundedNextFrame == true){
            _ignoreGroundedNextFrame = false;
            return;
        }
        
        if (_controller.isGrounded == true){
            State = PlayerState.Default; 
        }
    }
    private void AerialBehaviour()
    {
        if(State != PlayerState.Ascend && State != PlayerState.Descend) return;

        float vertical = _move.y;
        
        Vector3 input = new Vector3(_input.x, 0, _input.z);
        input = Quaternion.Euler(rot) * input;
        input *= db.playerAirSpeed * Time.deltaTime;
        _inertia += input;
        _inertia = Vector3.ClampMagnitude(_inertia, InertiaCap);

        _move = _inertia;
        _move.y = vertical;

    }
    private void DefaultBehaviour()
    {
        if(State != PlayerState.Default) return;
        float vertical = _move.y;
        
        _move = _input;
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed;

        _move.y = vertical;
        
        _move += Vector3.up * db.gravity;
    }
    //
    private void PlayerControlsSO_OnMove(Vector2 input, Vector2 raw)
    {
        _raw = raw;
        _input = new Vector3(input.x, 0, input.y);
    }    
    private void PlayerControlsSO_OnLook(Vector2 obj)
    {
        float sens = 0.1f;
        _mouseX += obj.x * sens;
        _mouseY += -obj.y * sens;

        _mouseY = Mathf.Clamp(_mouseY, db.minMouseY, db.maxMouseX);
        

    }
    private void PlayerControlsSO_OnJump()
    {
        if(State != PlayerState.Default) return;
        
        _ignoreGroundedNextFrame = true;
        _move.y = db.playerJumpHeight;
        _inertia = new Vector3(_move.x, 0, _move.z);
        InertiaCap = _inertia.magnitude;
    }
    //
    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        Debug.LogError(oldState + " -> " + newState);
    }
        
}
