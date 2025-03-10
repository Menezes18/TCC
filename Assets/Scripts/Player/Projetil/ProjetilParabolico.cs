using Mirror;
using UnityEngine;

public class ProjetilParabolico : NetworkBehaviour
{
    [SerializeField] private float tempoDeVida = 3f;
    private float multiplicadorVelocidade;
    private Vector3 velocity;
    private float gravidade;

    public void Inicializar(Vector3 velocityInicial, float grav, float velocidadeProjetil)
    {
        velocity = velocityInicial;
        gravidade = grav;
        multiplicadorVelocidade = velocidadeProjetil;
        Destroy(gameObject, tempoDeVida);
    }

    private void Update()
    {
        velocity.y += gravidade * Time.deltaTime * multiplicadorVelocidade;

        transform.position += velocity * Time.deltaTime * multiplicadorVelocidade;

        if (velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}