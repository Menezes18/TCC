using UnityEngine;

public class FallingObject : MonoBehaviour
{
    public float damageRadius = 1f;
    public LayerMask platformLayer;
    public GameObject impactEffect;

    void OnCollisionEnter(Collision collision)
    {
        // Verifica se atingiu uma plataforma
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius, platformLayer);
        
        foreach (Collider hitCollider in hitColliders)
        {
            FallingPlatformsGame gameController = FindObjectOfType<FallingPlatformsGame>();
            if (gameController != null)
            {
                gameController.PlatformHit(hitCollider.gameObject);
            }
        }
        
        // Efeito de impacto
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
}