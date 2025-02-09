using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour
{
    [Header("Configuração do Player")]
    [SerializeField] private PlayerControlSO _playerConfig;
    [SerializeField] private CharacterController _characterController;

    private Vector2 _input;
    private Vector3 _velocity;
    private bool _isGrounded;
    private PlayerStates _state;

    private void Update()
    {
        DetectGround();
        UpdateState();
        HandleMovement();
        ApplyGravity();
        _characterController.Move(_velocity * Time.deltaTime);
    }

    #region Input Callbacks 
    public void OnMove(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_state == PlayerStates.Default) 
        {
            _velocity.y = _playerConfig.jumpForce;
            _state = PlayerStates.Ascend;
        }
    }
    #endregion

    #region Estado do Jogador
    private void DetectGround()
    {
        _isGrounded = _characterController.isGrounded;
    }

    private void UpdateState()
    {
        if (_isGrounded)
        {
            _state = PlayerStates.Default;
        }
        else if (_velocity.y > 0)
        {
            _state = PlayerStates.Ascend;
        }
        else
        {
            _state = PlayerStates.Descend;
        }
    }
    #endregion

    #region Movimento
    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(_input.x, 0, _input.y);
        moveDirection = transform.rotation * moveDirection;

        if (_state == PlayerStates.Default)
        {
            _velocity.x = moveDirection.x * _playerConfig.speed;
            _velocity.z = moveDirection.z * _playerConfig.speed;
        }
        else
        {
            _velocity.x = moveDirection.x * _playerConfig.airSpeed;
            _velocity.z = moveDirection.z * _playerConfig.airSpeed;
        }
    }
    #endregion

    #region Gravidade e Pulo
    private void ApplyGravity()
    {
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; 
        }
        else
        {
            _velocity.y += _playerConfig.gravity * Time.deltaTime;
        }
    }
    #endregion
}
