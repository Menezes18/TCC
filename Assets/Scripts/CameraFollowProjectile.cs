using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowProjectileWithInput : MonoBehaviour
{
    [Header("Tags e Offsets")]
    public string projectileTag = "Projectile";
    public string cameraTag = "CameraTriler";
    public Vector3 offset = new Vector3(0f, 1f, -2f);

    [Header("Tempos e Velocidades")]
    public float followSpeed = 10f;
    public float restoreDelay = 2f;
    public float moveSpeed = 10f;      // velocidade em FreeFly
    

    [Header("FreeFly (Voo Livre)")]
    public float rotationSpeed = 120f;
    public float rotationSmoothTime = 0.1f;

    [Header("Smoothing")]
    public float panSmoothTime = 0.2f;
    public float moveSmoothTime = 0.2f;

    [Header("Debug/UI")]
    public bool showModeOnScreen = true;
    public Vector2 modeLabelPos = new Vector2(10f, 10f);

    Transform camTransform;
    Camera camComponent;
    Vector3 originalPos;
    Quaternion originalRot;
    bool originalSaved;

    Transform targetShot;
    bool isFollowing;

    enum CameraMode { Disabled, ManualPan, FollowProjectile, FreeFly }
    CameraMode currentMode = CameraMode.Disabled;

    Vector2 currentRotation;
    Vector2 rotationVelocity;
    Vector3 panVelocity;
    Vector3 freeFlyVelocity;

    bool adjustingPanSpeed = true; // true = scroll ajusta panSpeed; false = ajusta moveSpeed

    void Update()
    {
        if (camTransform == null) InitCamera();
        if (camTransform == null) return;

        var kb = Keyboard.current;
        if (kb.fKey.wasPressedThisFrame)
            SetMode(currentMode == CameraMode.Disabled ? CameraMode.ManualPan : CameraMode.Disabled);

        if (kb.vKey.wasPressedThisFrame && currentMode != CameraMode.Disabled)
            SetMode(CameraMode.FollowProjectile);

        switch (currentMode)
        {
            case CameraMode.ManualPan:
                UpdateManualPan(kb);
                TryStartFollow();
                break;
            case CameraMode.FollowProjectile:
                UpdateFollowProjectile();
                break;
            case CameraMode.FreeFly:
                UpdateFreeFly(kb);
                break;
        }
    }

    void InitCamera()
    {
        var camObj = GameObject.FindWithTag(cameraTag);
        if (camObj == null) return;
        camTransform = camObj.transform;
        camComponent = camObj.GetComponent<Camera>();
        camComponent.enabled = false;
        if (!originalSaved)
        {
            originalPos = camTransform.position;
            originalRot = camTransform.rotation;
            originalSaved = true;
            currentRotation = new Vector2(originalRot.eulerAngles.y, originalRot.eulerAngles.x);
        }
    }

    void SetMode(CameraMode mode)
    {
        currentMode = mode;
        switch (mode)
        {
            case CameraMode.Disabled:
                camComponent.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                ResetToOriginal();
                break;
            case CameraMode.ManualPan:
                camComponent.enabled = true;
                isFollowing = false;
                targetShot = null;
                Cursor.lockState = CursorLockMode.None;
                break;
            case CameraMode.FollowProjectile:
                camComponent.enabled = true;
                isFollowing = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case CameraMode.FreeFly:
                camComponent.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }

    void ResetToOriginal()
    {
        camTransform.position = originalPos;
        camTransform.rotation = originalRot;
    }

    void UpdateManualPan(Keyboard kb)
    {
        Vector2 input = Vector2.zero;
        if (kb.upArrowKey.isPressed)    input.y += 1f;
        if (kb.downArrowKey.isPressed)  input.y -= 1f;
        if (kb.leftArrowKey.isPressed)  input.x -= 1f;
        if (kb.rightArrowKey.isPressed) input.x += 1f;

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        Vector3 worldDir = camTransform.TransformDirection(dir);
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude > 0.01f)
        {
            Vector3 desiredPos = camTransform.position + worldDir.normalized * Time.deltaTime;
            camTransform.position = Vector3.SmoothDamp(camTransform.position, desiredPos, ref panVelocity, panSmoothTime);
        }

        if (kb.tKey.wasPressedThisFrame)
        {
            camTransform.position = new Vector3(camTransform.position.x, originalPos.y + 10f, camTransform.position.z);
            camTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    void TryStartFollow()
    {
        if (isFollowing) return;
        var proj = GameObject.FindWithTag(projectileTag);
        if (proj != null) Invoke(nameof(BeginFollow), 0f);
    }

    void BeginFollow()
    {
        var proj = GameObject.FindWithTag(projectileTag);
        if (proj == null) return;
        targetShot = proj.transform;
        isFollowing = true;
        currentMode = CameraMode.FollowProjectile;
        CancelInvoke(nameof(StopFollow));
        Invoke(nameof(StopFollow), restoreDelay);
    }

    void UpdateFollowProjectile()
    {
        if (!isFollowing || targetShot == null)
        {
            SetMode(CameraMode.ManualPan);
            return;
        }
        Vector3 desiredPos = targetShot.position + offset;
        camTransform.position = Vector3.Lerp(camTransform.position, desiredPos, followSpeed * Time.deltaTime);
        camTransform.LookAt(targetShot);
    }

    void StopFollow()
    {
        isFollowing = false;
        targetShot = null;
        ResetToOriginal();
        SetMode(CameraMode.ManualPan);
    }

    void UpdateFreeFly(Keyboard kb)
    {
        Vector2 delta = Mouse.current.delta.ReadValue() * rotationSpeed * Time.deltaTime;
        Vector2 targetRot = currentRotation + new Vector2(delta.x, -delta.y);
        currentRotation.x = Mathf.SmoothDamp(currentRotation.x, targetRot.x, ref rotationVelocity.x, rotationSmoothTime);
        currentRotation.y = Mathf.SmoothDamp(currentRotation.y, targetRot.y, ref rotationVelocity.y, rotationSmoothTime);
        camTransform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);

        Vector2 input = Vector2.zero;
        if (kb.upArrowKey.isPressed)    input.y += 1f;
        if (kb.downArrowKey.isPressed)  input.y -= 1f;
        if (kb.leftArrowKey.isPressed)  input.x -= 1f;
        if (kb.rightArrowKey.isPressed) input.x += 1f;

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        Vector3 worldDir = camTransform.TransformDirection(dir);
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude > 0.01f)
        {
            Vector3 desiredPos = camTransform.position + worldDir.normalized * moveSpeed * Time.deltaTime;
            camTransform.position = Vector3.SmoothDamp(camTransform.position, desiredPos, ref freeFlyVelocity, moveSmoothTime);
        }
    }

    void OnGUI()
    {
        if (!showModeOnScreen) return;
        GUI.Label(new Rect(modeLabelPos.x, modeLabelPos.y, 400, 20),
            $"Modo: {currentMode} | Ajustando: {(adjustingPanSpeed? "PanSpeed":"MoveSpeed")}");
    }
}
