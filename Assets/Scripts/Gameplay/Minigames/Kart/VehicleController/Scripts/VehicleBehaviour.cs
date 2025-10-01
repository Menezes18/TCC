using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleBehaviour : MonoBehaviour
{
        public event Action<bool> OnStartEngine; 
        public event Action<bool> OnEngineStarted;

        [Header("Components")]
        [Tooltip("Transform pai do modelo do veículo.")]
        [SerializeField] private Transform vehicleModel;
        [Tooltip("Atribua o collider esférico que está no mesmo GameObject que o Rigidbody. Dica: use a janela Vehicle Builder para preencher automaticamente ao criar um veículo.")]
        [FormerlySerializedAs("phsyicsSphere")]
        [SerializeField] private Rigidbody vehicleRigidbody;

        [Header("Vehicle")]
        [Tooltip("Atribua o Transform pai que compõe a carroceria do veículo.")]
        [SerializeField] private Transform vehicleBody;

        [Header("Vehicle Type")]
        [Tooltip("Escolha quantas rodas o veículo possui.")]
        [SerializeField] private VehicleType vehicleType;

        [Header("Wheels")]
        [Tooltip("Atribua o Transform da roda dianteira esquerda. Dica: se vir rotações incorretas ao dirigir, consulte a documentação para solução de problemas.")]
        [SerializeField] private Transform frontLeftWheel;
        [Tooltip("Atribua o Transform da roda dianteira direita. Dica: se vir rotações incorretas ao dirigir, consulte a documentação para solução de problemas.")]
        [SerializeField] private Transform frontRightWheel;
        [Tooltip("Atribua o Transform da roda traseira esquerda. Dica: se vir rotações incorretas ao dirigir, consulte a documentação para solução de problemas.")]
        [SerializeField] private Transform backLeftWheel;
        [Tooltip("Atribua o Transform da roda traseira direita. Dica: se vir rotações incorretas ao dirigir, consulte a documentação para solução de problemas.")]
        [SerializeField] private Transform backRightWheel;

        [Header("Settings")]
        [Tooltip("Crie e atribua um ScriptableObject de Configurações do Veículo; ele guarda os dados do veículo (aceleração, velocidade máx., derrapagem etc.). Dica: clicar no botão abaixo, no modo Play, permite ajustar e testar valores em tempo de execução.")]
        [SerializeField] private VehicleBehaviourSettings vehicleSettings;

        private Transform _container, _wheelFrontLeftParent, _wheelFrontRightParent;
        private bool _isWheelFrontLeftParentNotNull, _isWheelFrontRightParentNotNull;
        
        private float _speed, _speedTarget;
        private float _rotate, _tiltTarget, _twoWheelVehicleBodyTilt;
        private float _strafeSpeed, _strafeTarget, _strafeTilt;
        private float _wheelRadius, _wheelRotSpeed;
        private float _rayMaxDistance;

        // Required for rotating to match the ground
        private Vector3 _lastUp = Vector3.up;
        private bool _rotateToMatch = false;
        private float _elapsedTime = 0f;
        private Transform _fwd, _rear, _left, _right;
        private bool _groundForward, _groundRear;
        
        private Vector3 _rayOffsetForward, _rayOffsetRear;

        private bool _followGround;
        private bool _isBoosting;
        private bool _usingBoxCollider;

        private Vector3 _containerBase;
        private Vector3 _modelHeightOffGround;
        private List<Transform> _vehicleWheels;
        
        public VehicleType VehicleWheelCount { get => vehicleType; set => vehicleType = value; }
        public Transform VehicleModel { get => vehicleModel; set => vehicleModel = value; }
        public Rigidbody VehicleRigidbody { get => vehicleRigidbody; set => vehicleRigidbody = value; }
        
        public Transform VehicleBody { get => vehicleBody; set => vehicleBody = value; }
        public Transform FrontLeftWheel { get => frontLeftWheel; set => frontLeftWheel = value; }
        public Transform FrontRightWheel { get => frontRightWheel; set => frontRightWheel = value; }
        public Transform BackLeftWheel { get => backLeftWheel; set => backLeftWheel = value; }
        public Transform BackRightWheel { get => backRightWheel; set => backRightWheel = value; }

        public VehicleBehaviourSettings VehicleSettings { get => vehicleSettings; set => vehicleSettings = value; }

        public float Acceleration => VehicleSettings.acceleration;
        public float MaxSpeed { get; set; }
        public float BreakSpeed => VehicleSettings.breakSpeed;
        public float BoostSpeed => VehicleSettings.boostSpeed;
        public float MaxSpeedToStartReverse => VehicleSettings.maxSpeedToStartReverse;
        public float Steering { get; set; }
        public float MaxStrafingSpeed => VehicleSettings.maxStrafingSpeed;
        public float Gravity => VehicleSettings.gravity;
        public float Drift => VehicleSettings.drift;
        public float VehicleBodyTilt => VehicleSettings.vehicleBodyTilt;
        public float ForwardTilt => VehicleSettings.forwardTilt;
        public float StrafeTilt => vehicleSettings.strafeTilt;
        public bool TurnInAir => VehicleSettings.turnInAir;
        public bool TurnWhenStationary => VehicleSettings.turnWhenStationary;
        public bool TwoWheelTilt => VehicleSettings.twoWheelTilt;
        public bool StopSlopeSlide => VehicleSettings.stopSlopeSlide;
        public float RotateTarget { get; private set; }
        public bool NearGround { get; private set; }
        public bool OnGround { get; private set; }
        public LayerMask GroundMask => VehicleSettings.groundMask;
        public float DefaultMaxSpeed => VehicleSettings.maxSpeed;
        public float DefaultSteering => VehicleSettings.steering;
        public float CurrentSpeed => vehicleRigidbody.linearVelocity.magnitude;
        public bool IsBoosting => _isBoosting;
        public float GetVehicleVelocitySqrMagnitude => VehicleRigidbody.linearVelocity.sqrMagnitude;
        public Vector3 GetVehicleVelocity => VehicleRigidbody.linearVelocity;
        public bool EngineRunning { get; private set; }
        public bool IsReversing => _speedTarget < 0f;
        private bool StayFlat => VehicleSettings.stayFlatInAir;

        private void Awake()
        {
            GetRequiredComponents();
            CreateWheelList();
            SetVehicleSettings();
        }

        private void Start()
        {
            // Start engine directly based on settings (no audio/VFX coupling)
            EngineRunning = VehicleSettings.autoStartEngine;
        }

        private void GetRequiredComponents()
        {
            _usingBoxCollider = transform.parent.TryGetComponent(out Rigidbody _);

            if (vehicleBody == null) { Debug.LogError("Vehicle body has not been assigned on the VehicleBehaviour", gameObject); }

            if (frontLeftWheel != null)
            {
                _wheelFrontLeftParent = frontLeftWheel.parent;
                GetWheelRadius();
            }

            if (frontRightWheel != null) { _wheelFrontRightParent = frontRightWheel.parent; }
            
            _isWheelFrontRightParentNotNull = _wheelFrontRightParent != null;
            _isWheelFrontLeftParentNotNull = _wheelFrontLeftParent != null;

            _container = VehicleModel.GetChild(0);
            _containerBase = _container.localPosition;

            _modelHeightOffGround = new Vector3(0f, transform.localPosition.y, 0f);

            Collider vehicleCollider = _usingBoxCollider ? vehicleModel.GetComponentInChildren<Collider>() : VehicleRigidbody.GetComponent<Collider>();
            
            Bounds vehicleBounds = vehicleCollider.bounds;
            _rayOffsetForward = vehicleBounds.extents;
            _rayOffsetRear = -vehicleBounds.extents;

            var vehicleModelLocalPosition = VehicleModel.localPosition;
            _fwd = new GameObject("Forward Point").transform;
            _fwd.SetParent(VehicleModel);
            _fwd.position = transform.TransformPoint(new Vector3(vehicleModelLocalPosition.x, vehicleModelLocalPosition.y + 0.25f, vehicleModelLocalPosition.z + (_rayOffsetForward.z - 0.25f)));
            _rear = new GameObject("Rear Point").transform;
            _rear.SetParent(VehicleModel);
            _rear.position = transform.TransformPoint(new Vector3(vehicleModelLocalPosition.x, vehicleModelLocalPosition.y + 0.25f, _rayOffsetRear.z + 0.25f));
            _left = new GameObject("Left Point").transform;
            _left.SetParent(VehicleModel);
            _left.position = transform.TransformPoint(new Vector3(-0.25f, vehicleModelLocalPosition.y + 0.25f, vehicleModelLocalPosition.z));
            _right = new GameObject("Right Point").transform;
            _right.SetParent(VehicleModel);
            _right.position = transform.TransformPoint(new Vector3(0.25f, vehicleModelLocalPosition.y + 0.25f, vehicleModelLocalPosition.z));
        }

        private void CreateWheelList()
        {
            if (frontLeftWheel != null || frontRightWheel != null || backLeftWheel != null || BackRightWheel != null)
            {
                _vehicleWheels = new List<Transform>();
            
                if (frontLeftWheel != null) { _vehicleWheels.Add(frontLeftWheel); }
                if (frontRightWheel != null) { _vehicleWheels.Add(frontRightWheel); }
                if (backLeftWheel != null) { _vehicleWheels.Add(backLeftWheel); }
                if (backRightWheel != null) { _vehicleWheels.Add(backRightWheel); }
            }
        }

        private void GetWheelRadius()
        {
            Bounds wheelBounds = frontLeftWheel.GetComponentInChildren<Renderer>().bounds;
            _wheelRadius = wheelBounds.size.y;
        }

        public void SetVehicleSettings()
        {
            if (VehicleSettings == null)
            {
                Debug.LogError("Vehicle is missing Vehicle Settings asset.", gameObject);
                return;
            }

            MaxSpeed = DefaultMaxSpeed;
            Steering = DefaultSteering;

            _rayMaxDistance = Vector3.Distance(transform.position, VehicleModel.position) + 0.05f; // add 0.05f extra to the distance to account for vehicle tilt
            SetAngularDrag();
        }

        private void SetAngularDrag()
        {
            if (VehicleSettings.angularDrag == -1) { return; }

            vehicleRigidbody.angularDamping = VehicleSettings.angularDrag;
        }

        private void FixedUpdate()
        {
            Turn();
            TurnFrontWheels();
            TwoWheelVehicleTilt();
            
            Accelerate();
            Strafe();

            if (vehicleType != VehicleType.None) { SpinWheels(); }
            
            BodyTiltOnMovement();
            GroundVehicle();

            var vehicleModelPosition = VehicleModel.localPosition;
            Vector3 raycastOrigin = transform.TransformPoint(new Vector3(vehicleModelPosition.x, vehicleModelPosition.y + 0.5f, vehicleModelPosition.z));

            var vehiclePosition = transform.position;
            OnGround = Physics.Raycast(vehiclePosition, -transform.up, _rayMaxDistance + 0.15f, GroundMask);           
            NearGround = Physics.Raycast(raycastOrigin, -transform.up, out var hitNear, _rayMaxDistance + 1f, GroundMask);
            _followGround = VehicleSettings.looserGroundFollow ? NearGround : OnGround;
            
            Vector3 forwardRaycastOrigin = _fwd.position;
            Vector3 rearRaycastOrigin = _rear.position;
            Vector3 leftRaycastOrigin = _left.position;
            Vector3 rightRaycastOrigin = _right.position;
            Physics.Raycast(forwardRaycastOrigin, Vector3.down, out var forwardHit, _rayMaxDistance + 1f, GroundMask);
            Physics.Raycast(rearRaycastOrigin, Vector3.down, out var rearHit, _rayMaxDistance + 1f, GroundMask);
            Physics.Raycast(leftRaycastOrigin, Vector3.down, out var leftHit, _rayMaxDistance + 1f, GroundMask);
            Physics.Raycast(rightRaycastOrigin, Vector3.down, out var rightHit, _rayMaxDistance + 1f, GroundMask);
            
            Vector3 forwardGroundRaycast = _fwd.position;
            Vector3 rearGroundRaycast = _rear.position;
            _groundForward = Physics.Raycast(forwardGroundRaycast, _fwd.forward, 1f, GroundMask);
            _groundRear = Physics.Raycast(rearGroundRaycast, -_rear.forward, 1f, GroundMask);
            
            bool forwardHitNull = forwardHit.transform == null;
            bool rearHitNull = rearHit.transform == null;
            bool leftHitNull = leftHit.transform == null;
            bool rightHitNull = rightHit.transform == null;

            Vector3 direction;
            
            Vector3 forwardNormal = forwardHitNull == false ? forwardHit.point : new Vector3(forwardRaycastOrigin.x, forwardRaycastOrigin.y - (_rayMaxDistance + 1), forwardRaycastOrigin.z);
            Vector3 rearNormal = rearHitNull == false ? rearHit.point : new Vector3(rearRaycastOrigin.x, rearRaycastOrigin.y - (_rayMaxDistance + 1), rearRaycastOrigin.z);
            Vector3 leftNormal = leftHitNull == false ? leftHit.point : new Vector3(leftRaycastOrigin.x, leftRaycastOrigin.y, leftRaycastOrigin.z);
            Vector3 rightNormal = rightHitNull == false ? rightHit.point : new Vector3(rightRaycastOrigin.x, rightRaycastOrigin.y, rightRaycastOrigin.z);
            
            if ((OnGround || NearGround) || (_groundForward || _groundRear) && forwardHitNull == false && rearHitNull == false)
            {
                GetUpDirection(forwardNormal, rearNormal);
            }
            else 
            {
                direction = _lastUp;
            }
            
            if (StayFlat && OnGround == false && (forwardHitNull || rearHitNull))
            {
                forwardNormal = new Vector3(forwardRaycastOrigin.x, forwardRaycastOrigin.y - (_rayMaxDistance + 1), forwardRaycastOrigin.z);
                rearNormal = new Vector3(rearRaycastOrigin.x, rearRaycastOrigin.y - (_rayMaxDistance + 1), rearRaycastOrigin.z);
                GetUpDirection(forwardNormal, rearNormal);
            }
            // Editor debug visuals removed for cleaner runtime code
            
            if (direction != _lastUp)
            {
                _elapsedTime = 0f;
                if (OnGround && forwardHitNull == false && rearHitNull == false) { _elapsedTime = 0.3f; }
                if (_groundForward || _groundRear) { _elapsedTime = 0.15f; }
                _rotateToMatch = true;
            }
            
            if (_rotateToMatch == false) { direction = _lastUp; }
            else
            {
                if (_elapsedTime <= 0.35f) { _elapsedTime += Time.deltaTime; }
                else { _rotateToMatch = false; }
            }

            _lastUp = direction;
            
            VehicleModel.up = Vector3.Lerp(VehicleModel.up, direction, _elapsedTime);
            VehicleModel.Rotate(0f, transform.eulerAngles.y, 0f);

            if (_followGround)
            {
                VehicleRigidbody.AddForce(transform.forward * _speedTarget, ForceMode.Acceleration);
                VehicleRigidbody.AddForce(transform.right * _strafeTarget, ForceMode.Acceleration);
            }
            else
            {
                VehicleRigidbody.AddForce(transform.forward * (_speedTarget / 10f), ForceMode.Acceleration);
                VehicleRigidbody.AddForce(Vector3.down * Gravity, ForceMode.Acceleration);
            }
            
            Vector3 localVelocity = transform.InverseTransformVector(VehicleRigidbody.linearVelocity);
            localVelocity.x *= 0.9f + (Drift / 10f);
            
            if (NearGround) { VehicleRigidbody.linearVelocity = transform.TransformVector(localVelocity); }

            if (_usingBoxCollider == false) { transform.position = VehicleRigidbody.transform.position + _modelHeightOffGround; }

            if (StopSlopeSlide) { CounterSlopes(hitNear.normal); }

            void GetUpDirection(Vector3 fwd, Vector3 rear)
            {
                direction = (fwd - rear).normalized;
                direction = Vector3.Cross(direction, transform.right);
                Vector3 sideDirection = (leftNormal - rightNormal).normalized;
                sideDirection = Vector3.Cross(sideDirection, transform.forward);
                direction = new Vector3((direction.x + sideDirection.x), Mathf.Abs(direction.y), (direction.z + sideDirection.z));
            }
        }

        private void Accelerate()
        {
            _speedTarget = Mathf.Lerp(_speedTarget, _speed, Time.deltaTime * Acceleration);
            _speed = 0f;
        }
        
        private void Turn()
        {
            RotateTarget = Mathf.Lerp(RotateTarget, _rotate, Time.deltaTime * 4f);
            CalculateTilt(_rotate);
            _rotate = 0f;

            if (TurnWhenStationary == false && GetVehicleVelocitySqrMagnitude < 0.1f) { return; }

            float yRotation = _speedTarget < 0f ? transform.eulerAngles.y - RotateTarget : transform.eulerAngles.y + RotateTarget;

            if (_usingBoxCollider)
            {
                VehicleRigidbody.transform.rotation = Quaternion.Slerp(VehicleRigidbody.transform.rotation, Quaternion.Euler(new Vector3(0f, yRotation, 0f)), Time.deltaTime * 2.0f);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(new Vector3(0f, yRotation, 0f)), Time.deltaTime * 2.0f);
            }

            // Legacy reversing turn code removed to simplify
        }

        private void CalculateTilt(float tilt) => _tiltTarget = Mathf.Lerp(_tiltTarget, tilt, Time.deltaTime * 4f);

        private void Strafe()
        {
            _strafeTarget = Mathf.SmoothStep(_strafeTarget, _strafeSpeed, Time.deltaTime * Acceleration);
            _strafeSpeed = 0f;

            float zRotation = StrafeTilt == 0f ? 0f : _strafeTarget / StrafeTilt;
            vehicleBody.localRotation = Quaternion.Slerp(vehicleBody.localRotation, Quaternion.Euler(new Vector3(0f, 0f, -zRotation)), Time.deltaTime * 4f);
        }

        private void TurnFrontWheels()
        {
            if (_isWheelFrontLeftParentNotNull) { _wheelFrontLeftParent.localRotation = Quaternion.Euler(_wheelFrontLeftParent.localRotation.x, RotateTarget / 2f, 0f); }
            if (_isWheelFrontRightParentNotNull) { _wheelFrontRightParent.localRotation = Quaternion.Euler(_wheelFrontRightParent.localRotation.x, RotateTarget / 2f, 0f); }
        }

        private void BodyTiltOnMovement()
        {
            if (TurnWhenStationary == false && GetVehicleVelocitySqrMagnitude < 0.1f) { return; }

            float xRotation = ForwardTilt == 0f ? 0f : _speedTarget / ForwardTilt;
            float zRotation = VehicleBodyTilt == 0f ? RotateTarget / 6f : (RotateTarget / 6f) * VehicleSettings.vehicleBodyTilt;

            vehicleBody.localRotation = Quaternion.Slerp(vehicleBody.localRotation, Quaternion.Euler(new Vector3(xRotation, 0f, zRotation)), Time.deltaTime * 4f);
        }

        private void TwoWheelVehicleTilt()
        {
            if (TwoWheelTilt == false) { return; }

            _twoWheelVehicleBodyTilt = -_tiltTarget / 1.5f;

            _container.localPosition = _containerBase + new Vector3(0f, Mathf.Abs(_twoWheelVehicleBodyTilt) / 2000f, 0f);
            _container.localRotation = Quaternion.Slerp(_container.localRotation, Quaternion.Euler(0f, RotateTarget / 8f, _twoWheelVehicleBodyTilt), Time.deltaTime * 10f);
        }

        private void SpinWheels()
        {
            _wheelRotSpeed = Vector3.Dot(transform.forward, VehicleRigidbody.linearVelocity);

            float distanceTraveled = _wheelRotSpeed * Time.deltaTime;
            float rotationInRadians = distanceTraveled / _wheelRadius;
            float rotationInDegrees = rotationInRadians * Mathf.Rad2Deg;

            for (int i = 0; i < _vehicleWheels.Count; i++)
            {
                _vehicleWheels[i].Rotate(rotationInDegrees, 0f, 0f);
            }
        }

        private void GroundVehicle()
        {
            // Keeps vehicle grounded when standing still
            if (_speed == 0f && GetVehicleVelocitySqrMagnitude < 4f)
            {
                VehicleRigidbody.linearVelocity = Vector3.Lerp(VehicleRigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 2.0f);
            }
        }

        private void CounterSlopes(Vector3 groundNormal)
        {
            if (_speedTarget > 5f) { return; }

            float multiplier = _usingBoxCollider == false ? 1f : -3f;
            Vector3 carForward = _usingBoxCollider == false ? transform.right : transform.forward;
            Vector3 gravity = Physics.gravity;
            Vector3 directionOfFlat = Vector3.Cross(-gravity, groundNormal).normalized; //the direction that if you head in you wouldnt change altitude
            Vector3 directionOfSlope = Vector3.Cross(directionOfFlat, groundNormal); //the direction down the slope
            float affectOfGravity = Vector3.Dot(gravity, directionOfSlope); // returns 1 on a cliff face, 0 on a plane
            float affectOfWheelAlignment = Mathf.Abs(Vector3.Dot(carForward, directionOfSlope)); // returns 1 if facing down or up the slope, 0 if 90 degrees to slope
            affectOfWheelAlignment = affectOfWheelAlignment >= VehicleSettings.slideThreshold ? 1f : affectOfWheelAlignment;
            VehicleRigidbody.AddForce(-directionOfSlope * (affectOfWheelAlignment * affectOfGravity * multiplier), ForceMode.Acceleration);
        }

        /// <summary>
        /// Toggle the engine on/ off by passing in 'true' or 'false'
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="invokeEvent"></param>
        public void ToggleEngine(bool enable, bool invokeEvent)
        {
            // Simplified: directly set engine state (no audio events)
            EngineRunning = enable;
        }

        /// <summary>
        /// Call this once the engine has started, for example after any audio SFX for the engine turning on/ off
        /// </summary>
        /// <param name="enable"></param>
        public void InvokeEngineStarted(bool enable)
        {
            EngineRunning = enable;
            OnEngineStarted?.Invoke(enable);
        }

        /// <summary>
        /// Change the MaxSpeed of the vehicle. Use DefaultMaxSpeed to return to the original MaxSpeed
        /// </summary>
        /// <param name="speedPenalty"></param>
        public void MovementPenalty(float speedPenalty) => MaxSpeed = speedPenalty;
        
        /// <summary>
        /// Change the Steering speed of the vehicle. Use DefaultSteering to return to the original Steering
        /// </summary>
        /// <param name="steerPenalty"></param>
        public void SteeringPenalty(float steerPenalty) => Steering = steerPenalty;

        // Input controls	

        /// <summary>
        /// Move the vehicle forward
        /// </summary>
        public void ControlAcceleration()
        {
            if (EngineRunning == false) { return; }
            
            // Add 6 to MaxSpeed, RigidBody reaches a speed of (MaxSpeed - 6).
            // EG: MaxSpeed of 30 will result in a speed of ~24. A MaxSpeed of 6 will result in speed of 0
            // Adding 6 to MaxSpeed here until a better solution found
            if (_isBoosting == false) { _speed = MaxSpeed + 6f; }
            else { _speed = BoostSpeed; }
        }

        /// <summary>
        /// Slow down and reverse
        /// </summary>
        public void ControlBrake()
        {
            if (GetVehicleVelocitySqrMagnitude > MaxSpeedToStartReverse) 
            { 
                _speed -= BreakSpeed; 
            }
            else 
            { 
                if (EngineRunning == false) { return; }
                _speed = -MaxSpeed; 
            }
        }

        /// <summary>
        /// Turn left (float -1) or right (float 1). 
        /// </summary>
        /// <param name="direction"></param>
        public void ControlTurning(float direction)
        {
            if (NearGround || TurnInAir)
            {
                _rotate = Steering * direction;
            }
        }

        /// <summary>
        /// Move sideways, left (float -1) or right (float 1)
        /// </summary>
        /// <param name="direction"></param>
        public void ControlStrafing(float direction)
        {
            if (EngineRunning == false) { return; }
            
            _strafeSpeed = MaxStrafingSpeed * (direction * 2f);
        }
        
        /// <summary>
        /// Make the vehicle jump, force must take into account the mass of the vehicle.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="forceMode"></param>
        public void Jump(float force, ForceMode forceMode = ForceMode.Impulse) => VehicleRigidbody.AddForce(Vector3.up * force, forceMode);

        /// <summary>
        /// Sets isBoosting to true. Set your boost speed in the VehicleSettings asset
        /// </summary>
        public void Boost() => _isBoosting = true;

        /// <summary>
        /// Performs a timed boost, pass in a float for how long the boost should last in seconds
        /// </summary>
        /// <param name="boostLength"></param>
        public void OneShotBoost(float boostLength)
        {
            if (_isBoosting == false)
            {
                StartCoroutine(BoostTimer(boostLength));
            }
        }

        private IEnumerator BoostTimer(float boostLength)
        {
            Boost();

            yield return new WaitForSeconds(boostLength);

            StopBoost();
        }

        /// <summary>
        /// Sets isBoosting to false
        /// </summary>
        public void StopBoost() => _isBoosting = false;

        /// <summary>
        /// Set the position and rotation of the vehicle. This will also set the speed and turning to 0
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void SetPosition(Vector3 position, Quaternion rotation)
        {
            VehicleRigidbody.linearVelocity = Vector3.zero;
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.position = position;

            _speed = _speedTarget = _rotate = 0.0f;

            if (_usingBoxCollider == false)
            {
                VehicleRigidbody.Sleep();
                transform.SetPositionAndRotation(position, rotation);
                VehicleRigidbody.WakeUp();
            }
        }
    }
