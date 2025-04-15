using UnityEngine;
using Mirror;

public class ColorChange : NetworkBehaviour
{
    public Material redMaterial;
    public Material blueMaterial;
    public Material greenMaterial;

    [SyncVar(hook = nameof(OnMaterialNameChanged))]
    private string currentMaterialName = "default";

    // Chamado automaticamente quando o SyncVar muda
    private void OnMaterialNameChanged(string oldValue, string newValue)
    {
        ApplyMaterial(newValue);
    }

    public void SetMaterial(string materialName)
    {
        if (isServer)
        {
            currentMaterialName = materialName;
        }
    }

    private void ApplyMaterial(string materialName)
    {
        Material targetMaterial = null;

        if (materialName == "red") targetMaterial = redMaterial;
        else if (materialName == "blue") targetMaterial = blueMaterial;
        else targetMaterial = greenMaterial;

        if (targetMaterial == null)
        {
            Debug.LogWarning("Material não encontrado: " + materialName);
            return;
        }

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
                rend.material = new Material(targetMaterial);
                count++;
            }
        }
    }
}
