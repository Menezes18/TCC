using UnityEngine;
using Mirror;
using System.Linq;

public class TriggerPersoZone : NetworkBehaviour
{
    public int index = 0;
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            PersoManager persoManager = other.GetComponent<PersoManager>();
            if (persoManager != null)
            {
                persoManager.SetHat(index); // ativa o chapéu com índice que eu botei
            }
        }
    }
}