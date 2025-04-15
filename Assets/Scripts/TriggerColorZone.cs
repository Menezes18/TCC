using UnityEngine;
using Mirror;

public class TriggerColorZone : NetworkBehaviour
{
    [SerializeField] private string materialToSend = "red"; // ou "blue"

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            ColorChange colorChange = other.GetComponent<ColorChange>();
            if (colorChange != null)
            {
                colorChange.SetMaterial(materialToSend); // só muda a SyncVar
            }
        }
    }
}
