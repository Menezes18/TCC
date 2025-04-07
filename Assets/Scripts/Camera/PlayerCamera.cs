using System;
using Cinemachine;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private bool _isCursorLocked = true;
    private PlayerScript _playerScript;

    private void Awake()
    {
        _playerScript = GetComponent<PlayerScript>();
    }

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
        
        SetCursorState(true);        
        PlayerControlSO.EventOnLook += OnLook;
        PlayerControlSO.EventOnCursor += EventOnCursor;
    }


    private void OnDestroy()
    {
        if (isOwned)
        {
            PlayerControlSO.EventOnCursor -= EventOnCursor;
            PlayerControlSO.EventOnLook -= OnLook;
        }
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
    
    private void EventOnCursor(InputAction.CallbackContext obj)
    {
        if (!obj.performed) return;
        
        Debug.LogError($"EventOnCursor chamado.{_isCursorLocked}");
        _isCursorLocked = !_isCursorLocked;
        SetCursorState(_isCursorLocked);
    }
    private void SetCursorState(bool locked)
    {
        Debug.Log(locked);
        _isCursorLocked = locked;
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        //Depois melhorar isso
        _playerScript.SetInputEnabled(new string[] { "Move"}, locked);
        _playerScript.SetInputEnabled(new string[] { "Look"}, locked );
        _playerScript.SetInputEnabled(new string[] { "Jump"}, locked );
        _playerScript.SetInputEnabled(new string[] { "Push"}, locked );
        _playerScript.SetInputEnabled(new string[] { "Shoot"}, locked);
        Debug.LogError("EventOnCursor" + locked);
        Debug.Log($"Cursor definido para: Visible={Cursor.visible}, LockState={Cursor.lockState}");
    }
    public void SwitchTarget(Transform newTarget)
    {
        if (_cam != null)
        {
            _cam.Follow = newTarget;
            _cam.LookAt = newTarget;
        }
    }
}