using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ChaoMae : MonoBehaviour
{
    [SerializeField]
    float _speed = 5;
    public float speed 
    { 
        get { return _speed; } 
        protected set { _speed = value; } 
    }

    [SerializeField]
    float _tempo = 5;
    public float tempo 
    { 
        get { return _tempo; } 
        protected set { _tempo = value; } 
    }
    
    [SerializeField]
    bool _chaoTirado = false;
    public bool chaoTirado 
    { 
        get { return _chaoTirado; } 
        protected set { _chaoTirado = value; } 
    }

    public Vector3 posIncial;

    void Awake()
    {
        posIncial = transform.position;
    }

    public abstract void tiraChao();

    public abstract void poeChao();
}
