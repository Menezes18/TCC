using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "ScriptableObject", menuName = "Player/PlayerControlsSO")]
public class PlayerControlsSO : ScriptableObject{

    public event Action<Vector2> OnMove; 
    public event Action<Vector2> OnLook; 
    public event Action OnPush; 
    public event Action OnJump;
    public event Action OnThrow;
    public event Action OnThrowCancel;

    public void Move(Vector2 move){ OnMove?.Invoke(move); }
    public void Look(Vector2 look){ OnLook?.Invoke(look); }
    public void Jump() { OnJump?.Invoke(); }
    public void Push() { OnPush?.Invoke(); }
    public void Throw() { OnThrow?.Invoke(); }
    public void ThrowCancel() { OnThrowCancel?.Invoke(); }

}