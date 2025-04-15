using UnityEngine;
using Mirror;

public class TriggerColorZone : NetworkBehaviour
{
    [SerializeField] private Material materialSend;
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            ColorChange colorChange = other.GetComponent<ColorChange>();
            colorChange.materialReceive = materialSend;
            if (colorChange != null)
            {
                colorChange.RpcChangeColor(); // Chama no próprio jogador
                Debug.Log("Chamei");
            }
        }
    }
}
