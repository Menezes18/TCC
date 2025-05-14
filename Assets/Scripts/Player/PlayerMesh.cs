using UnityEngine;

public class PlayerMesh : MonoBehaviour
{
    
    [SerializeField] Database db;
    [SerializeField] SkinnedMeshRenderer[] _smr;

    public void SetMaterialColor(int id)
    {
        _smr[0].material = db.playerColors[id].material;
    }
    
    
    public void SetVisibility(bool logic)
    {
        foreach (var skinmesh in _smr)
        {
            skinmesh.enabled = logic;
        
        }
    }
}
