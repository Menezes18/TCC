using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target; // O alvo a ser seguido (o jogador)
    public float smoothSpeed = 0.125f; // Velocidade de suavização
    public Vector3 offset; // Distância da câmera em relação ao jogador

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Opcional: Fazer a câmera olhar para o jogador
            transform.LookAt(target);
        }
    }
}