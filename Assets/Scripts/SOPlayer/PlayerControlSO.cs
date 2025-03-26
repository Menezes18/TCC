using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "NewPlayerControl", menuName = "Player/ControlSO")]
public class PlayerControlSO : ScriptableObject{
    public float inputAcel;
    public float inputGravity;
    
    public Action<Vector2> EventMove;
    public Action<Vector2> EventOnCustomMove;
    
    public Action<InputAction.CallbackContext> EventOnJump;

    public Action<Vector2> EventOnLook;

    public Action<InputAction.CallbackContext> EventOnPush;
    public Action<InputAction.CallbackContext> EventOnShoot;
    public Action<InputAction.CallbackContext> EventOnCursor;
    public void OnShoot(InputAction.CallbackContext context)
    {
        EventOnShoot?.Invoke(context);
    }

    public void OnPush(InputAction.CallbackContext context)
    {
        if(context.performed)
            EventOnPush?.Invoke(context);
    }
    public void OnCursor(InputAction.CallbackContext context)
    {
        if(context.performed)
            EventOnCursor?.Invoke(context);
    }
    public void OnLook(InputAction.CallbackContext context){
        EventOnLook?.Invoke(context.ReadValue<Vector2>());
    }
    public void OnCustomMove(Vector2 move){
        EventOnCustomMove?.Invoke(move);
    }
    public void OnJump(InputAction.CallbackContext context){
        if (context.performed)
            EventOnJump?.Invoke(context);
    }

    public void OnMove(InputAction.CallbackContext context){
        EventMove?.Invoke(context.ReadValue<Vector2>());
    }
}