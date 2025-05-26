using UnityEngine;

public class VfxClone : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    public void Cloner()
    {
        // Ativa o objeto original (opcional se já estiver ativo)
        gameObject.SetActive(true);

        // Cria o clone
        GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
        
        // Remove o script do clone para evitar que ele também se clone
        Destroy(clone.GetComponent<VfxClone>());

        // Desparenteia até não ter mais pai
        while (clone.transform.parent != null)
        {
            clone.transform.SetParent(null);
        }

        // Destroi o clone após o tempo de vida
        Destroy(clone, lifetime);
    }
}
