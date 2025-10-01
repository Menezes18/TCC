using System;
using UnityEngine;

[RequireComponent(typeof(VehicleBehaviour))]
public class VehicleEffects : MonoBehaviour
{
        [Tooltip("Se verdadeiro, o sistema de partículas sempre emitirá quando o veículo estiver em movimento")]
        [SerializeField] private bool alwaysSmoke = false;
        [SerializeField] private bool alwaysReverseEffects = true;

        [SerializeField] private DriftSettings driftSettings;
        [Header("Obsolete")]
        [Obsolete("Field has been replaced by DriftSettings and will be removed in a future release.")]
        [SerializeField] private float skidSpeedThreshold = 1.25f;
        [Obsolete("Field has been replaced by DriftSettings and will be removed in a future release.")]
        [SerializeField] private float skidAngleThreshold = 20.0f;

        private VehicleBehaviour _vehicleBehaviour;
        private ParticleSystem[] _exhaustEffect;
        private TrailRenderer[] _trails;
        private bool _shouldEmmit = false;

        private void Awake() => GetRequiredComponents();

        private void GetRequiredComponents()
        {
            _vehicleBehaviour = GetComponent<VehicleBehaviour>();
            _exhaustEffect = GetComponentsInChildren<ParticleSystem>();
            _trails = GetComponentsInChildren<TrailRenderer>();

            if (driftSettings != null) { return; }
            
            driftSettings = ScriptableObject.CreateInstance<DriftSettings>();
            driftSettings.SetDriftSettings(skidSpeedThreshold, skidAngleThreshold, 1f);
        }

        private void Update() => Effects();
        private void LateUpdate() => UpdateEmitting();
        
        private void Effects()
        {
            Exhaust();

            for (int i = 0; i < _trails.Length; i++)
            {
                Trail(_trails[i]);
            }
        }

        private void UpdateEmitting()
        {
            bool emmitCheck = _vehicleBehaviour.OnGround && 
                                _vehicleBehaviour.GetVehicleVelocitySqrMagnitude > (_vehicleBehaviour.MaxSpeed / driftSettings.SkidSpeed) && 
                                (Vector3.Angle(_vehicleBehaviour.GetVehicleVelocity, _vehicleBehaviour.VehicleModel.forward) > driftSettings.SkidAngle || alwaysSmoke);

            bool reverseCheck = _vehicleBehaviour.GetVehicleVelocitySqrMagnitude > (_vehicleBehaviour.MaxSpeed / driftSettings.SkidReverseSpeed) && 
                                (Vector3.Angle(_vehicleBehaviour.GetVehicleVelocity, _vehicleBehaviour.VehicleModel.forward) > driftSettings.SkidAngle || alwaysReverseEffects);
            
            _shouldEmmit = _vehicleBehaviour.IsReversing == false ? emmitCheck : reverseCheck;
        }

        private void Exhaust()
        {
            for (int i = 0; i < _exhaustEffect.Length; i++)
            {
                ParticleSystem.EmissionModule smokeEmission = _exhaustEffect[i].emission;
                smokeEmission.enabled = _shouldEmmit;
            }
        }

        private void Trail(TrailRenderer trail) => trail.emitting = _shouldEmmit;
        
        public void AddDriftSettings(DriftSettings newSettings) => driftSettings = newSettings;
    
}
