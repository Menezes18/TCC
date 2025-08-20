using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Chao/ChaoMaeSO")]
public class ChaoMaeSo : ScriptableObject
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

}
