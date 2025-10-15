using UnityEngine;

[CreateAssetMenu(fileName = nameof(DriftSettings), menuName = "Vehicle/Drift Settings", order = 5)]
public class DriftSettings : ScriptableObject
{
        [Tooltip("A velocidade máxima é dividida por este valor; valores menores exigem ir mais rápido para ativar as derrapagens.")]
        [SerializeField] private float skidSpeedThreshold = 1.25f;
        [Tooltip("Defina o ângulo mínimo de curva para ativar as derrapagens.")]
        [SerializeField] private float skidAngleThreshold = 20.0f;
        [Tooltip("A velocidade máxima é dividida por este valor; valores menores exigem ir mais rápido para ativar as derrapagens em marcha à ré.")]
        [SerializeField] private float skidReverseSpeedThreshold = 1f;
        
        public float SkidSpeed => skidSpeedThreshold;
        public float SkidAngle => skidAngleThreshold;
        public float SkidReverseSpeed => skidReverseSpeedThreshold;

        public void SetDriftSettings(float speed, float angle, float reverseSpeed)
        {
            skidSpeedThreshold = speed;
            skidAngleThreshold = angle;
            skidReverseSpeedThreshold = reverseSpeed;
        }
    
}
