using UnityEngine;
using Mirror;

public class ColorChange : NetworkBehaviour
{
    public Material materialReceive;

    [ClientRpc]
    public void RpcChangeColor()
    {
        Debug.Log("Respondi");
        Transform meshChild = transform.Find("MacacoAnimacoes");
        if (meshChild == null)
        {
            Debug.LogWarning("Mesh 'MacacoAnimacoes' não encontrada.");
            return;
        }

        int count = 0;
        foreach (Transform bodyPart in meshChild)
        {
            if (count >= 4) break;

            Renderer rend = bodyPart.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(materialReceive);
                count++;
            }
        }
    }
}
