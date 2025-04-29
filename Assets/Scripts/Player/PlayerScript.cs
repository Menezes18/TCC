using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerScript : NetworkBehaviour{
    
    [SerializeField] private PlayerInputSO PlayerInputSO;

    private void Start()
    {
        PlayerInputSO.OnMove += 
    }
}
