using System;
using Mirror;
using UnityEngine;

public class ProjetilParabolico : NetworkBehaviour
{
    [SerializeField] private float tempoDeVida = 3f;
    private float multiplicadorVelocidade;
    private Vector3 velocity;
    private float gravidade;
    private NetworkIdentity shooterIdentity;
    public void Inicializar(Vector3 velocityInicial, float grav, float velocidadeProjetil, NetworkIdentity shooter)
    {
        velocity = velocityInicial;
        gravidade = grav;
        multiplicadorVelocidade = velocidadeProjetil;
        shooterIdentity = shooter;
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

    private void OnTriggerEnter(Collider other)
    {
        // Em P2P a colisão pode acontecer em qualquer cliente que seja host
        if (other.CompareTag("Player")) {
            NetworkIdentity hitPlayerIdentity = other.GetComponent<NetworkIdentity>();
        
            if (hitPlayerIdentity != null && shooterIdentity != null && 
                hitPlayerIdentity.netId == shooterIdentity.netId) {
                return; 
            }
            PlayerScript hitPlayer = other.GetComponent<PlayerScript>();
            if (hitPlayer != null && isServer) {
                hitPlayer.TargetOnHitByShot(hitPlayer.connectionToClient);
            }
        }
    }
}