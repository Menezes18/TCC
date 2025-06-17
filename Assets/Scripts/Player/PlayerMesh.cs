using UnityEngine;

[System.Serializable]
public struct MeshMaterialTarget
{
    public SkinnedMeshRenderer renderer;
    public int materialIndex;
}
public class PlayerMesh : MonoBehaviour
{
    
    [SerializeField] Database db;
    [SerializeField] MeshMaterialTarget[] meshMaterial;

    public void SetMaterialColor(int id)
    {
        foreach (var target in meshMaterial){
            var materials = target.renderer.materials;
            if(target.materialIndex < materials.Length){

                materials[target.materialIndex] = db.playerColors[id].material;
                target.renderer.materials = materials;
            }
            else{
                Debug.LogError("NÃO TEM MATERIAL COM ESSE INDICE BURRO " + target.renderer.name);
            }
        }
    }
    
    
    public void SetVisibility(bool logic)
    {
        foreach (var skinmesh in meshMaterial)
        {
            skinmesh.renderer.enabled = logic;
        
        }
    }
}
