using UnityEngine;

[CreateAssetMenu(fileName = nameof(VehicleBehaviourSettings), menuName = "Vehicle/Vehicle Settings", order = 3)]
public class VehicleBehaviourSettings : ScriptableObject
{
        [Header("Parameters")]
        [Tooltip("Quão rápido o veículo acelera.")]
        [Range(1f, 12f)] public float acceleration = 5f;
        [Tooltip("Velocidade máxima que o veículo pode atingir.")]
        [Range(1f, 100f)] public float maxSpeed = 30f;
        [Tooltip("Quão rápido o veículo deve desacelerar ao frear.")]
        [Range(1f, 15f)] public float breakSpeed = 5f;
        [Tooltip("Define a velocidade máxima durante o turbo.")]
        [Range(5f, 200f)] public float boostSpeed = 60f;
        [Tooltip("Quando mudar de frear para ré usando o valor ao quadrado da velocidade do veículo.")]
        [Range(1f, 500f)] public float maxSpeedToStartReverse = 150f;
        [Tooltip("Controla o ângulo de viragem do veículo (quão fechada é a curva). Quanto menor o valor, maior o arco da curva.")]
        [Range(20f, 160f)] public float steering = 80f;
        [Tooltip("Velocidade máxima do movimento lateral (strafe) esquerda/direita.")]
        [Range(1f, 40f)] public float maxStrafingSpeed = 15f;
        [Tooltip("Define a gravidade, alterando quão rápido o veículo cai quando está no ar.")]
        [Range(0f, 20f)] public float gravity = 10f;
        [Tooltip("Quão facilmente o veículo desliza nas curvas; quanto menor o valor, mais difícil é derrapar.")]
        [Range(0f, 1f)] public float drift = 1f;
        [Tooltip("Quanto o veículo inclina no eixo Z ao virar.")]
        [Range(0f, 3f)] public float vehicleBodyTilt = 0f;
        [Tooltip("Defina 0 para sem inclinação. Valores entre 0 e 1 podem causar rotação indesejada. Valores maiores resultam em inclinação mais sutil.")]
        [Range(0f, 10f)] public float forwardTilt = 8f;
        [Tooltip("Defina 0 para sem inclinação. Valores maiores resultam em inclinação mais sutil.")]
        [Range(0f, 10f)] public float strafeTilt = 8f;
        [Tooltip("Define o arrasto angular do Rigidbody. Isso desacelera o veículo ao subir inclinações. -1 mantém o valor definido no Rigidbody.")]
        public float angularDrag = -1f;

        [Header("Switches")]
        [Tooltip("O motor deve iniciar ligado? Se falso, você precisará de uma forma para o jogador ligar o motor.")]
        public bool autoStartEngine = true;
        [Tooltip("Ativar o 'looser ground follow' permite que o veículo saia do chão com rampas menores. Veja a documentação para ilustrações.")]
        public bool looserGroundFollow = true;
        [Tooltip("Ative para manter o veículo nivelado ao cair de um penhasco. Deixe desativado para que ele gire e aponte para o chão enquanto cai.")]
        public bool stayFlatInAir = false;
        [Tooltip("Permite virar enquanto está no ar.")]
        public bool turnInAir = true;
        [Tooltip("Permite virar quando não há movimento.")]
        public bool turnWhenStationary = true;
        [Tooltip("Adiciona inclinação lateral extra ao virar. Ideal para motos.")]
        public bool twoWheelTilt = false;
        [Tooltip("Impede que o veículo deslize ladeira abaixo quando estiver perpendicular à inclinação.")]
        public bool stopSlopeSlide = true;
        [Tooltip("Valor normalizado para ajustar quando o veículo começa a escorregar numa rampa. 1 é o padrão (para o deslizamento a 90°); 0,1 exigirá que o veículo esteja apontado diretamente para cima/baixo da rampa para rolar.")]
        [Range(0.1f, 1.0f)] public float slideThreshold = 1f;
        [Header("Ground Layer")]
        [Tooltip("A Layer necessária para o veículo poder se mover.")]
        public LayerMask groundMask = 1 << 0;
    
}
