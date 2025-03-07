using Mirror;
using UnityEngine;

public class ProjetilParabolico : NetworkBehaviour
{
    [SerializeField] private float tempoDeVida = 3f;

    private Vector3 velocity;
    private float gravidade;

    public void Inicializar(Vector3 velocityInicial, float grav)
    {
        velocity = velocityInicial;
        gravidade = grav;
        Destroy(gameObject, tempoDeVida);
    }

    private void Update()
    {
        velocity.y += gravidade * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}