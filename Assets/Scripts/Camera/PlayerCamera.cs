using System;
using Cinemachine;
using Mirror;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Configuração da Câmera")]
    [SerializeField] private PlayerControlSO PlayerControlSO;
    [SerializeField] private Transform _cameraTarget;

    private CinemachineVirtualCamera _cam;
    private float _mouseX, _mouseY;
    public float TargetHeight = 1f;
    public PlayerScript PlayerScript;
    private Vector2 targetLook;
    public Vector2 Clamp = new Vector2(-75, 75);
    public float CurrentCameraYRotation => _mouseX;


    private void Start()
    {
        if (!isOwned)
        {
            
            Transform camTransform = transform.Find("Virtual Camera");
            if (camTransform != null)
            {
                camTransform.gameObject.SetActive(false);
            }
            return;
        }

        
        _cam = GetComponentInChildren<CinemachineVirtualCamera>();
        if (_cam != null)
        {
            _cam.gameObject.SetActive(true);
            _cam.Follow = _cameraTarget;
            _cam.LookAt = _cameraTarget;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        PlayerControlSO.EventOnLook += OnLook;
    }

    private void LateUpdate()
    {
        if (!isOwned) return;
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
        _cameraTarget.transform.position = PlayerScript.transform.position + Vector3.up * TargetHeight;
    }

    private void UpdateCameraRotation()
    {
        _cameraTarget.transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
        transform.rotation = Quaternion.Euler(0, _mouseX, 0);
    }
}