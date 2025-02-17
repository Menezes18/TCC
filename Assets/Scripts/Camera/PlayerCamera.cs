using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Configuração da Câmera")]
    [SerializeField] private PlayerControlSO PlayerControlSO;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0.5f, 0); 
    
    private Camera _cam;
    private float _mouseX, _mouseY;
    
    private void Start()
    {
        if(base.isOwned == false) return;
        _cam = Camera.main;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        PlayerControlSO.EventOnLook += OnLook;
    }

    

    private void LateUpdate()
    {
        if(base.isOwned == false) return;
        UpdateCameraPosition();
        UpdateCameraRotation();
    }

    public void OnLook(Vector2 input)
    {
        Vector2 mouseDelta = input * 0.1f;
        
        _mouseX += mouseDelta.x;
        _mouseY = Mathf.Clamp(_mouseY - mouseDelta.y, -75f, 75f);
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = transform.position + cameraOffset;
        _cam.transform.position = targetPosition;
    }

    private void UpdateCameraRotation()
    {
        transform.rotation = Quaternion.Euler(0, _mouseX, 0);
        _cam.transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
    }
}