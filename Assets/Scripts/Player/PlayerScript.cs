using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerScript : NetworkBehaviour{
    
    
    [SerializeField] PlayerControlsSO PlayerControlsSO;
    [SerializeField] CharacterController _controller;
    [SerializeField] Database db;

    private Transform _cam;

    public Vector3 rot => new Vector3(0, _cam.transform.rotation.eulerAngles.y, 0);

    private Vector3 _input;
    private Vector3 _move;
    private Vector3 _raw;
    float _mouseX, _mouseY;

    private void Start()
    {
        PlayerControlsSO.OnMove += PlayerControlsSO_OnMove;
        PlayerControlsSO.OnLook += PlayerControlsSO_OnLook;
        
        _cam = Camera.main.transform;
    }

    private void Update()
    {
        float vertical = _move.y;
        
        _move = _input;
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed;
        _move *= Time.deltaTime;
        
        _move += Vector3.up * db.gravity;
        
        if (_controller.isGrounded)
            _move.y = db.gravityGrounded;
        
        _controller.Move(_move);

        transform.rotation = Quaternion.Euler(rot);

    }

    private void LateUpdate()
    {
        
        Vector3 newRot = new Vector3(_mouseY, _mouseX, 0);
        _cam.transform.rotation = Quaternion.Euler(newRot);
        _cam.transform.position = transform.position + _cam.rotation * db.orbitalOffset;

    }

    private void PlayerControlsSO_OnMove(Vector2 input, Vector2 raw)
    {
        _raw = raw;
        _input = new Vector3(input.x, 0, input.y);
    }    
    private void PlayerControlsSO_OnLook(Vector2 obj)
    {
        float sens = 0.1f;
        _mouseX += obj.x * sens;
        _mouseY += -obj.y * sens;

        _mouseY = Mathf.Clamp(_mouseY, db.minMouseY, db.maxMouseX);
        

    }
        
}
