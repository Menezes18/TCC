using UnityEngine;

public class PlayerMesh : MonoBehaviour
{
    
    [SerializeField] Database db;
    [SerializeField] SkinnedMeshRenderer _smr;

    public void SetMaterialColor(int id)
    {
        _smr.material = db.playerColors[id].material;
    }
}
