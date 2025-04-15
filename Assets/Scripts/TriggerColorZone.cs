using UnityEngine;
using Mirror;

public class TriggerColorZone : NetworkBehaviour
{
    [SerializeField] private string materialToSend = "red"; // ou "blue"

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return; // Só pra verificar o status

        if (other.CompareTag("Player")) //Verificação padrão pro meu trigger
        {
            ColorChange colorChange = other.GetComponent<ColorChange>(); // pegar o script
            if (colorChange != null)
            {
                colorChange.SetMaterial(materialToSend); // só muda a SyncVar
            }
        }
    }
}
