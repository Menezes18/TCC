using UnityEngine;
using Mirror;

public abstract class ChaoMae : NetworkBehaviour
{
    [SyncVar]
    protected bool chaoTirado = false;
    
    public ChaoMaeSo dataChao;
    public Vector3 posIncial;
    
    void Awake()
    {
        posIncial = transform.position;
    }

    public abstract void tiraChao();
    public abstract void poeChao();
}