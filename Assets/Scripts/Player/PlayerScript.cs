using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] HUDSO HUDSO;
    
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

            if (value == PlayerStatus.Throw){
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

    [SerializeField] private Transform _cam;

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
    readonly int _MOVEX = Animator.StringToHash("MoveX");
    readonly int _MOVEY = Animator.StringToHash("MoveY");

    private float _staggerTimer;
    private float _pushCooldown;
    
    private float _rollTimer;
    private float _rollCooldown;

    private float _blindTimer;

    private float BlindTimer{
        get => _blindTimer;
        set{
            if(_blindTimer == value) return;
            _blindTimer = value;
        }
    }
    
    private float _throwCooldown;

    private PlayerInput _playerInput;
    
    //Public
    public bool IsAirborne => State == PlayerState.Ascend || State == PlayerState.Descend;

    public Transform cameraTarget;

    [SyncVar(hook = nameof(OnStaggerChanged))]
    private bool isStaggered;
    
    private float sensibilidade = 4;
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
        cameraTarget = transform;
        
        // Cache
        _playerInput = GetComponent<PlayerInput>();
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
        
        if (_throwCooldown > 0) _throwCooldown -= Time.deltaTime;
        
        if(_blindTimer > 0) _blindTimer -= Time.deltaTime;

        float blindWeight = CustomMath.ConvertRange(_blindTimer, db.playerBlindDuration, 0);
        float blindRange = db.playerBlindCurve.Evaluate(blindWeight);
        HUDSO.SetBlindAlpha(blindRange);
        
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
       
       if (Status == PlayerStatus.Throw){
           if (_throwCooldown <= 0){
               
               Status = PlayerStatus.Default;
           }
       }
       if (Status == PlayerStatus.Blinded){
           if (_throwCooldown <= 0){
               
               Status = PlayerStatus.Default;
           }
       }
       
        StaggerBehaviour();
        AerialBehaviour();
        DefaultBehaviour();
 
        _animator.SetFloat(_MOVEX, _input.x, 0.1f, Time.deltaTime);
        _animator.SetFloat(_MOVEY, _input.z, 0.1f, Time.deltaTime);
        
        
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
        
        Quaternion camRotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _cam.rotation = camRotation;
        Vector3 desiredPos = cameraTarget.position + _cam.transform.rotation * db.orbitalOffset;

        Vector3 dir = desiredPos - cameraTarget.position;
        float maxDist = db.orbitalOffset.magnitude;
        
        if (Physics.SphereCast(cameraTarget.position, db.cameraSphereRadius, dir.normalized,
                out RaycastHit hit, maxDist, db.cameraColliderMash,
                QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Clamp(hit.distance - db.cameraSphereRadius, 0.1f, maxDist);
            _cam.transform.position = cameraTarget.position + dir.normalized * safeDist;
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
        // _mouseX += obj.x * sens;
        // _mouseY += -obj.y * sens;
        //
        // _mouseY = Mathf.Clamp(_mouseY, db.minMouseY, db.maxMouseX);
        _yaw   += obj.x * sensibilidade * Time.deltaTime;
        _pitch -= obj.y * sensibilidade * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, db.minMouseY, db.maxMouseX);

    }
    private void PlayerControlsSO_OnJump()
    {
        if(State != PlayerState.Default) return;
        
        if (_move.y > 0)
            State = PlayerState.Ascend;
        else if (_move.y < db.gravityGrounded)
            State = PlayerState.Descend;
        
        _ignoreGroundedNextFrame = true;
        _move.y = db.playerJumpHeight;
        _inertia = new Vector3(_move.x, 0, _move.z);
        InertiaCap = _inertia.magnitude;
    }
    private void PlayerControlsSO_OnPush()
    {
        if(State == PlayerState.Stagger) return;
        if(Status != PlayerStatus.Default || Status == PlayerStatus.Blinded) return;
        
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
        if(Status != PlayerStatus.Default) return;
        if(Status == PlayerStatus.Throw) return;
        if (_throwCooldown > 0) return;
        
        
        Status = PlayerStatus.ThrowPrepare;

    }
    
    private void PlayerControlsSO_OnThrowCancel()
    {
        if (State == PlayerState.Stagger) return;
        if(Status == PlayerStatus.Pushing) return;
        if(Status == PlayerStatus.Throw) return;
        Status = PlayerStatus.Throw;

        
        Vector3 origin = transform.TransformPoint(db.projectileLocalOffset);
        Vector3 direction = _cam.forward;

        PrefabInstancer.singleton.CmdSpawnProjectile(
            origin,
            direction,
            this.netIdentity
        );
        _throwCooldown = db.playerThrowCooldown;
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
        if (dmgType == DamageType.Poop){

            Status = PlayerStatus.Blinded;
            _blindTimer = db.playerBlindDuration;
            
            return;
        }
            
        
        
        //

        State = PlayerState.Stagger;
        
        //isStaggered = true;
        
        Debug.DrawRay(transform.position, dir * 5, Color.cyan, 5);
        
        
        Vector3 horizontal = new Vector3(_move.x, 0, _move.z);
        Vector3 final = horizontal + dir * db.playerPushStrength;
        _inertia = final;
        InertiaCap = final.magnitude;
        _move.y = db.playerStaggerHeight;
        _staggerTimer = db.playerStaggerStunDuration;
        
        //StartCoroutine(ClearStagger(db.playerStaggerStunDuration));

    }

    [Server]
    private IEnumerator ClearStagger(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        isStaggered = false;
    }
    
    public void OnStaggerChanged(bool oldValue, bool newValue)
    {
        _staggerIndicator.gameObject.SetActive(newValue);
    }
    #region Sensibilidade
        
        public void CmdChangeSensitivity(float normalized)
        {
            sensibilidade = Mathf.Lerp(0f, 25f, normalized);
            Debug.Log($"[Server] Sensibilidade ajustada para {sensibilidade}");
        }
        public void RequestSensitivityChange(float normalized)
        {
            if (!isOwned) return;
            CmdChangeSensitivity(normalized);
        }

    #endregion
    #region System Network
    [Command]
    public void Die()
    {
        State = PlayerState.Death;
        _controller.enabled = false; 
        RpcSpectate();
    }

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
    }

    private Transform FindSpectatorTarget()
    {
        PlayerScript[] players = FindObjectsOfType<PlayerScript>();
        foreach (var player in players)
        {
            if (player != this && player.State != PlayerState.Death)
            {
                return player.transform;
            }
        }
        return null;
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

        Debug.Log($"[CLIENT] Player {netId} respawned at {position}");
    }
    #endregion
}
