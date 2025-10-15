using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "PlayerControlsSO", menuName = "Player/PlayerControlsSO")]
public class PlayerControlsSO : ScriptableObject {

    public event Action<Vector2, Vector2> OnMove;
    public event Action<Vector2> OnLook;
    public event Action<float> OnRotatePanel;
    public event Action<float> OnZoomPanel;
    public event Action OnPush;
    public event Action OnJump;
    public event Action OnThrow;
    public event Action OnThrowCancel;
    public event Action OnRoll;

    //
    public event Action OnMenu;
    public event Action OnCursor;
    public event Action OnDebug;
    public event Action OnClosePanel;  



    public void Move(Vector2 move, Vector2 raw) { OnMove?.Invoke(move, raw); }
    public void Look(Vector2 look) { OnLook?.Invoke(look); }
    public void Jump() { OnJump?.Invoke(); }
    public void Push() { OnPush?.Invoke(); }
    public void Throw() { OnThrow?.Invoke(); }
    public void Roll() { OnRoll?.Invoke(); }
    public void ThrowCancel() { OnThrowCancel?.Invoke(); }
    public void MenuCelular() { OnMenu?.Invoke(); }
    // public void Cursor() { OnCursor?.Invoke(); }
    
    public void Debug() { OnDebug?.Invoke(); }

    // Panel-specific helpers
    public void RotatePanel(float x) { OnRotatePanel?.Invoke(x); }
    public void ZoomPanel(float scroll) { OnZoomPanel?.Invoke(scroll); }
    public void ClosePanel() { OnClosePanel?.Invoke(); }

    public void EnableCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

}
