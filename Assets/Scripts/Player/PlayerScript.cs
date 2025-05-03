using Mirror;
using UnityEngine;

public enum PlayerState{
    Default,
    Ascend,
    Descend,
    Stagger,
    Roll,
    Death,
}

public enum PlayerStatus{
    Default,
    Blinded,
    Pushing,
    ThrowPrepare,
    Throw,
}
public class PlayerScript : NetworkBehaviour, IDamageable
{
    [SerializeField] Database db;
    [SerializeField] PlayerControlsSO PlayerControlsSO;
    
    [SerializeField] CharacterController _controller;
    [SerializeField] Animator _animator;
    [SerializeField] NetworkAnimator _networkAnimator;
    
    [SerializeField] PlayerState _state;
    PlayerState State{
        get {return _state;}
        set{
            if(_state == value){return;}
            OnStateChanged(_state, value);
            _animator.SetInteger(_STATE, (int)value);
            _state = value;
        }
    }
    
    [SerializeField] PlayerStatus _status;

    PlayerStatus Status{
        get {return _status;}
        set{
            if(_status == value) return;
            
            Debug.LogError(_status + " -> " + value);
            _animator.SetInteger(_STATUS, (int)value);
            _status = value;

            if (value != PlayerStatus.Pushing) return;
            
            // NetworkAnimator não replica trigger
            // Então tem que passar sempre nos 2
            // animator --> trigger
            // networkAnimator --> trigger
            _animator.SetTrigger("push");
            _networkAnimator.SetTrigger("push");
            
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

    private Vector3 _roll;
    
    private float _mouseX, _mouseY;
    private bool _ignoreGroundedNextFrame;
    
    readonly int _STATE = Animator.StringToHash("state");
    readonly int _STATUS = Animator.StringToHash("status");

    private float _staggerTimer;
    private float _pushCooldown;
    private float _rollTimer;
    private float _rollCooldown;
    
    public bool IsAirborne => State == PlayerState.Ascend || State == PlayerState.Descend;
    
    private void Start()
    {
        if(!this.isOwned) return;
        
        PlayerControlsSO.OnMove += PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook += PlayerControlsSO_OnLook;
        PlayerControlsSO.OnJump += PlayerControlsSO_OnJump;
        PlayerControlsSO.OnPush += PlayerControlsSO_OnPush;
        PlayerControlsSO.OnRoll += PlayerControlsSO_OnRoll;
        PlayerControlsSO.OnThrow += PlayerControlsSO_OnThrow;
        PlayerControlsSO.OnThrowCancel += PlayerControlsSO_OnThrowCancel;

        
        _cam = Camera.main.transform;
    }

    private void OnDestroy()
    {
        PlayerControlsSO.OnMove -= PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook -= PlayerControlsSO_OnLook;
        PlayerControlsSO.OnJump -= PlayerControlsSO_OnJump;
        PlayerControlsSO.OnPush -= PlayerControlsSO_OnPush;
        PlayerControlsSO.OnRoll -= PlayerControlsSO_OnRoll;
        PlayerControlsSO.OnThrow -= PlayerControlsSO_OnThrow;
        PlayerControlsSO.OnThrowCancel -= PlayerControlsSO_OnThrowCancel;
    }

    private void Update()
    {
        if(!this.isOwned) return;
        
        //
        if(_pushCooldown > 0 ) _pushCooldown -= Time.deltaTime;
        
        if(_staggerTimer > 0) _staggerTimer -= Time.deltaTime;
        
        if(_rollTimer > 0) _rollTimer -= Time.deltaTime;
        
        if(_rollCooldown > 0) _rollCooldown -= Time.deltaTime;
        //
        
        AerialDetection();

        
       /* if (State == PlayerState.Roll){
            float vertical = _move.y;

            float weight = CustomMath.Normalized01(_rollTimer, db.playerMaxAirSpeed, 0f);
            float range = db.playerRollCurve.Evaluate(weight);
            Debug.LogError(weight + " -> " + range);
            Vector3 horizontal = _roll;
            horizontal = Quaternion.Euler(rot) * horizontal;
            horizontal *= db.playerRollSpeed * range;

            _move = horizontal;

            _move.y = vertical;

            if (_rollTimer <= 0) State = GetDefaultStatus();
        }*/
        
        StaggerBehaviour();
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
        if(!this.isOwned) return;
        
        Vector3 newRot = new Vector3(_mouseY, _mouseX, 0);
        _cam.transform.rotation = Quaternion.Euler(newRot);

        Vector3 desiredPos = transform.position + _cam.transform.rotation * db.orbitalOffset;

        Vector3 dir = desiredPos - transform.position;
        float maxDist = db.orbitalOffset.magnitude;
        
        if (Physics.SphereCast(transform.position, db.cameraSphereRadius, dir.normalized,
                out RaycastHit hit, maxDist, db.cameraColliderMash,
                QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Clamp(hit.distance - db.cameraSphereRadius, 0.1f, maxDist);
            _cam.transform.position = transform.position + dir.normalized * safeDist;
        }
        else
        {
            _cam.transform.position = desiredPos;
        }
    }
    //
    private void AerialDetection()
    {
        if(State == PlayerState.Stagger) return;
        if(State == PlayerState.Roll) return;
        
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
        
        if(_staggerTimer > 0) return;
        
        // Exit Condition
        if (_controller.isGrounded == false) return;
        
        State = PlayerState.Default;
        
        
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
    public void SetDefaultState()
    {
        if (!_controller.isGrounded){
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
        if(_move.y > 0)
            return PlayerState.Ascend;
        if (_move.y < -1)
            return PlayerState.Descend;
        
        return PlayerState.Default;
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
    private void PlayerControlsSO_OnPush()
    {
        if(State == PlayerState.Stagger) return;
        if(Status != PlayerStatus.Default) return;
        
        if(_pushCooldown > 0) return;
        
        Status = PlayerStatus.Pushing;
        _pushCooldown = db.playerPushCooldownTimer;
    }
    private void PlayerControlsSO_OnRoll()
    {
        if(IsAirborne) return;
        if(State == PlayerState.Stagger) return;
        if(_rollCooldown > 0) return;

        _roll = new Vector3(_raw.x, 0, _raw.y);

        if (_roll.magnitude == 0)
            _roll = Vector3.forward;

        State = PlayerState.Roll;
        _rollTimer = db.playerRollDuration;

    }
    private void PlayerControlsSO_OnThrow()
    {
        if(State == PlayerState.Stagger) return;
        
    }

    private void PlayerControlsSO_OnThrowCancel()
    {
        if(State == PlayerState.Stagger) return;
        
    }

    //
    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        Debug.LogError(oldState + " -> " + newState);

        if (oldState == PlayerState.Roll)
            _rollCooldown = db.playerRollCooldownDuration;
    }
    
    //
    [Server]
    public void ReceiveDamage(DamageType dmgType, Vector3 dir)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        RpcReceiveDamage(coon, dmgType, dir);
    }

    [TargetRpc]
    public void RpcReceiveDamage(NetworkConnection coon, DamageType dmgType, Vector3 dir)
    {
        //

        State = PlayerState.Stagger;
        Debug.DrawRay(transform.position, dir * 5, Color.cyan, 5);
        
        Vector3 horizontal = new Vector3(_move.x, 0, _move.z);
        Vector3 final = horizontal + dir * db.playerPushStrength;
        _inertia = final;
        InertiaCap = final.magnitude;
        _move.y = db.playerStaggerHeight;
        _staggerTimer = db.playerStaggerStunDuration;

    }
}
