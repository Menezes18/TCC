using UnityEngine;
using Mirror;

public class ColorChange : NetworkBehaviour
{
    //Fiz por string, mas é código de preguiçoso, formatar pra Enum ou algo do tipo
    public Material redMaterial;
    public Material blueMaterial;
    public Material greenMaterial;

    [SyncVar(hook = nameof(OnMaterialNameChanged))] // persistencia de dados
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

    private void ApplyMaterial(string materialName) // aplicação do material
    {
        Material targetMaterial = null;

        //O certo seria ter o do green mas no caso é minha excessão :P
        if (materialName == "red") targetMaterial = redMaterial;
        else if (materialName == "blue") targetMaterial = blueMaterial;
        else targetMaterial = greenMaterial;

        if (targetMaterial == null) //Se não achar material
        {
            Debug.LogWarning("Material não encontrado: " + materialName);
            return;
        }

        Transform meshChild = transform.Find("MacacoAnimacoes");
        if (meshChild == null) // se não achar a mesh
        {
            Debug.LogWarning("Mesh 'MacacoAnimacoes' não encontrada.");
            return;
        }

        int count = 0; // pintar o macaco todo
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
