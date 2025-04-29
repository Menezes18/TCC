
using static UnityEngine.InputSystem.InputAction;
using UnityEngine;
using UnityEngine.InputSystem;

    public class PlayerControls : MonoBehaviour {
        
        public PlayerInputSO PlayerInputSO;
        public PlayerControlsSO PlayerControlsSO;
        public Database db;

        [SerializeField]
        PlayerInput _playerInput;
        float _rawX, _rawY;
        float _x, _y;

        private float _mouse;
        private void Start(){
            PlayerInputSO.OnMove += PlayerInputSO_OnMove;
            PlayerInputSO.OnLook += PlayerInputSO_OnLook;
            PlayerInputSO.OnJump += PlayerInputSO_OnJump;
            PlayerInputSO.OnPush += PlayerInputSO_OnPush;
            PlayerInputSO.OnThrow += PlayerInputSO_OnThrow;
        }
        private void Update(){
            
          
            if(_rawX != 0)
                _x = Mathf.MoveTowards(_x, _rawX, playerInputSoinputAcel * Time.deltaTime);
            else
                _x = Mathf.MoveTowards(_x, 0, playerInputSo.inputGravity * Time.deltaTime);
            
            if(_rawY != 0)
                _y = Mathf.MoveTowards(_y, _rawY, playerInputSo.inputAcel * Time.deltaTime);
            else
                _y = Mathf.MoveTowards(_y, 0, playerInputSo.inputGravity * Time.deltaTime);
            
            playerInputSo.OnCustomMove(new Vector2(_x, _y));
        }
        private void PlayerInputSO_OnMove(CallbackContext obj)
        {
            _rawX = obj.ReadValue<Vector2>().x;
            _rawY = obj.ReadValue<Vector2>().y;
            
        }
        private void PlayerInputSO_OnLook(CallbackContext obj)
        {
            if(obj.performed){
                PlayerControlsSO.Look(obj.ReadValue<Vector2>());
            }
        }
        private void PlayerInputSO_OnJump(CallbackContext obj)
        {
            if(obj.performed){
                PlayerControlsSO.Jump();
            }

        }


        private void PlayerInputSO_OnPush(CallbackContext obj)
        {
            if(obj.performed){
                PlayerControlsSO.Push();
            }
        }
        
        private void PlayerInputSO_OnThrow(CallbackContext obj)
        {
            if (obj.canceled){
                PlayerControlsSO.ThrowCancel();
                return;
            }
            if(obj.performed){
                PlayerControlsSO.Throw();
            }
        }





        private void OnDestroy(){
            //PlayerInputSO.EventMove -= EventMove;
        }
       
        private void EventMove(Vector2 obj){
            obj = obj.normalized;
            
            _rawX = obj.x;
            _rawY = obj.y;
        }
        public Vector2 GetInput()
        {
            return new Vector2(_x, _y);
        }
        
    }
