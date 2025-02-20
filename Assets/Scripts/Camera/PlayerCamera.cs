using System;
using Mirror;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Configuração da Câmera")]
    [SerializeField] private PlayerControlSO PlayerControlSO;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0.5f, 0); 
    private Camera _cam;
    private float _mouseX, _mouseY;
    public Transform CameraTarget;
    public float TargetHeight = 1f;
    public PlayerScript PlayerScript;
    private Vector2 targetLook;
    public Vector2 Clamp = new Vector2(-75, 75);
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
        _mouseY = Mathf.Clamp(_mouseY - mouseDelta.y, Clamp.x, Clamp.y);
    }

    private void UpdateCameraPosition()
    {
        CameraTarget.transform.position = PlayerScript.transform.position + Vector3.up * TargetHeight;
    }

    private void UpdateCameraRotation()
    {
        CameraTarget.transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
         transform.rotation = Quaternion.Euler(0, _mouseX, 0);
        // _cam.transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
    }
}