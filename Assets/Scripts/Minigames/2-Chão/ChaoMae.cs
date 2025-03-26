using UnityEngine;
using Mirror;

public abstract class ChaoMae : NetworkBehaviour
{
    [SyncVar]
    [SerializeField]
    private bool _chaoTirado = false;
    public bool chaoTirado 
    { 
        get { return _chaoTirado; } 
        protected set { _chaoTirado = value; } 
    }
    public ChaoMaeSo dataChao;
    public Vector3 posIncial;
    void Awake()
    {
        posIncial = transform.position;
    }

    public abstract void tiraChao();
    public abstract void poeChao();
}