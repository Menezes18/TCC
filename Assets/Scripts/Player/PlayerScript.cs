using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour
{
    [Header("Configuração do Player")]
    [SerializeField]  Database db;
    [SerializeField]  PlayerControlSO _playerSO;
    [SerializeField]  CharacterController _characterController;

    public Vector3 rot => new Vector3(0, Camera.main.transform.rotation.eulerAngles.y, 0);
    
    
    private Vector2 _input;
    private Vector3 _move;
    private bool _isGrounded;
    private PlayerStates _state;
    

    private void Start(){
        _playerSO.EventOnCustomMove += EventOnCustomMove;
        _playerSO.EventOnJump += EventOnJump;
    }

    
    
    private void Update()
    {
        _move = new Vector3(_input.x, 0, _input.y);
        _move = Quaternion.Euler(rot) * _move;
        _move *= db.playerSpeed;
        _characterController.Move(_move * Time.deltaTime);
    }
    
    private void EventOnJump(InputAction.CallbackContext obj){
        throw new NotImplementedException();
    }

    private void EventOnCustomMove(Vector2 obj){
        _input = obj;
    }
}
