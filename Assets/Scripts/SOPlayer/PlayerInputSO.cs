using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "PlayerInputSO", menuName = "Player/PlayerInputSO")]
public class PlayerInputSO : ScriptableObject{
    public float inputAcel;
    public float inputGravity;
    
    public event Action<CallbackContext> OnMove;
    public void Move(CallbackContext obj) {this.OnMove?.Invoke(obj);}
    
    public event Action<CallbackContext> OnLook;
    public void Look(CallbackContext obj) {this.OnLook?.Invoke(obj);}
    
    public event Action<CallbackContext> OnJump;
    public void Jump(CallbackContext obj) {this.OnJump?.Invoke(obj);}
    
    public event Action<CallbackContext> OnPush;
    public void Push(CallbackContext obj) {this.OnPush?.Invoke(obj);}
    
    public event Action<CallbackContext> OnThrow;
    public void Throw(CallbackContext obj) {this.OnThrow?.Invoke(obj);}
    
    public event Action<CallbackContext> OnMenuCelular;
    public void MenuCelular(CallbackContext obj) {this.OnMenuCelular?.Invoke(obj);}
    
    public event Action<CallbackContext> OnCursor;
    public void Cursor(CallbackContext obj) {this.OnCursor?.Invoke(obj);}


}