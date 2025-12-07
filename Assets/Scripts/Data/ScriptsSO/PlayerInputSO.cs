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
    
    // n vou usar, so para aprender
    public event Action<CallbackContext> OnRoll;
    public void Roll(CallbackContext obj) {this.OnRoll?.Invoke(obj);}
    
    public event Action<CallbackContext> OnDebug; 
    
    public void Debug(CallbackContext obj) {this.OnDebug?.Invoke(obj);}

    public event Action<CallbackContext> OnScroll;
    public void Scroll(CallbackContext obj) {this.OnScroll?.Invoke(obj);}

    public event Action<CallbackContext> OnCancel;
    public void Cancel(CallbackContext obj) {this.OnCancel?.Invoke(obj);}
    
    /// <summary>
    /// Limpa todos os eventos registrados.
    /// Deve ser chamado quando o jogador desconecta para evitar referências a objetos destruídos.
    /// </summary>
    public void ClearAllEvents()
    {
        OnMove = null;
        OnLook = null;
        OnJump = null;
        OnPush = null;
        OnThrow = null;
        OnMenuCelular = null;
        OnCursor = null;
        OnRoll = null;
        OnDebug = null;
        OnScroll = null;
        OnCancel = null;
        
        UnityEngine.Debug.Log("[PlayerInputSO] All events cleared");
    }
    
    /// <summary>
    /// Chamado quando o ScriptableObject é habilitado (início do jogo ou após recompilação).
    /// </summary>
    private void OnEnable()
    {
        // Limpa eventos ao iniciar para garantir estado limpo
        // Isso é útil especialmente no editor onde os SOs persistem entre play sessions
        #if UNITY_EDITOR
        ClearAllEvents();
        #endif
    }
}