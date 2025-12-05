
using static UnityEngine.InputSystem.InputAction;
using UnityEngine;
using UnityEngine.InputSystem;

    public class PlayerControls : MonoBehaviour {
        
        [SerializeField] PlayerInputSO PlayerInputSO;
        [SerializeField] PlayerControlsSO PlayerControlsSO;
        [SerializeField] Database db;
        [SerializeField] PlayerScript playerScript;

        [SerializeField]
        PlayerInput _playerInput;
        float _rawX, _rawY;
        float _x, _y;

        private float _mouse;
        private bool _hasSubscribed = false;
        
        private void Start(){
            
            playerScript = GetComponent<PlayerScript>();
            
            if (!playerScript.isLocalPlayer) return;
            
            SubscribeToInputEvents();
        }
        
        /// <summary>
        /// Inscreve nos eventos de input. Limpa eventos anteriores para evitar duplicação.
        /// </summary>
        private void SubscribeToInputEvents()
        {
            if (_hasSubscribed) return;
            
            // Limpa eventos antigos que podem ter ficado de sessões anteriores
            // Isso é importante quando o host sai e cria uma nova sala
            if (PlayerInputSO != null)
            {
                PlayerInputSO.ClearAllEvents();
            }
            
            if (PlayerControlsSO != null)
            {
                PlayerControlsSO.ClearAllEvents();
            }
            
            // Agora inscreve nos eventos
            PlayerInputSO.OnMove += PlayerInputSO_OnMove;
            PlayerInputSO.OnLook += PlayerInputSO_OnLook;
            PlayerInputSO.OnJump += PlayerInputSO_OnJump;
            PlayerInputSO.OnPush += PlayerInputSO_OnPush;
            PlayerInputSO.OnThrow += PlayerInputSO_OnThrow;
            PlayerInputSO.OnMenuCelular += PlayerInputSO_OnMenuCelular;
            PlayerInputSO.OnRoll += PlayerInputSO_OnRoll;
            PlayerInputSO.OnDebug += PlayerInputSOOnOnDebug;
            PlayerInputSO.OnScroll += PlayerInputSO_OnScroll;
            PlayerInputSO.OnCancel += PlayerInputSO_OnCancel;
            
            _hasSubscribed = true;
            Debug.Log("[PlayerControls] Subscribed to input events");
        }
        
        /// <summary>
        /// Remove as inscrições dos eventos de input.
        /// </summary>
        private void UnsubscribeFromInputEvents()
        {
            if (!_hasSubscribed) return;
            
            if (PlayerInputSO != null)
            {
                PlayerInputSO.OnMove  -= PlayerInputSO_OnMove;
                PlayerInputSO.OnLook  -= PlayerInputSO_OnLook;
                PlayerInputSO.OnJump  -= PlayerInputSO_OnJump;
                PlayerInputSO.OnPush  -= PlayerInputSO_OnPush;
                PlayerInputSO.OnThrow -= PlayerInputSO_OnThrow;
                PlayerInputSO.OnRoll  -= PlayerInputSO_OnRoll;
                PlayerInputSO.OnMenuCelular -= PlayerInputSO_OnMenuCelular;
                PlayerInputSO.OnScroll -= PlayerInputSO_OnScroll;
                PlayerInputSO.OnCancel -= PlayerInputSO_OnCancel;
                PlayerInputSO.OnDebug -= PlayerInputSOOnOnDebug;
            }
            
            _hasSubscribed = false;
            Debug.Log("[PlayerControls] Unsubscribed from input events");
        }

        private void OnDestroy(){
            if (playerScript != null && playerScript.isLocalPlayer) {
                UnsubscribeFromInputEvents();
            }
        }
        
        private void OnDisable()
        {
            // Também limpa quando desabilitado para evitar problemas
            if (playerScript != null && playerScript.isLocalPlayer) {
                UnsubscribeFromInputEvents();
            }
        }
        
        
        private void Update(){

            if (playerScript.panel || playerScript.isFrozen || playerScript.UILocked)
            {
                _rawX = 0;
                _rawY = 0;
                _x = 0;
                _y = 0;
                PlayerControlsSO.Move(new Vector2(_x, _y), new Vector2(_rawX, _rawY));
                return;
            }

            if (_rawX == 0)
                _x = Mathf.MoveTowards(_x, 0, db.inputGravity * Time.deltaTime);
            else
                _x = Mathf.MoveTowards(_x, _rawX, db.inputAccel * Time.deltaTime);
            
            if(_rawY == 0)
                _y = Mathf.MoveTowards(_y, 0, db.inputGravity * Time.deltaTime);
            else
                _y = Mathf.MoveTowards(_y, _rawY, db.inputAccel * Time.deltaTime);
            
            PlayerControlsSO.Move(new Vector2(_x, _y), new Vector2(_rawX, _rawY));
        }
        private void PlayerInputSO_OnMove(CallbackContext obj)
        {
            if (playerScript.panel || playerScript.isFrozen || playerScript.UILocked)
            {
                if (playerScript.panel)
                {
                    Vector2 val = obj.ReadValue<Vector2>();
                    PlayerControlsSO.RotatePanel(val.x);
                }
                _rawX = 0;
                _rawY = 0;
                return;
            }
            _rawX = obj.ReadValue<Vector2>().x;
            _rawY = obj.ReadValue<Vector2>().y;
            
        }
        private void PlayerInputSO_OnLook(CallbackContext obj)
        {
            if (!playerScript.isLocalPlayer) return;
            if (playerScript.UILocked) return; 
            PlayerControlsSO.Look(obj.ReadValue<Vector2>());
        }
        private void PlayerInputSO_OnJump(CallbackContext obj)
        {
            if (playerScript.UILocked) return;
            if(obj.performed){
                PlayerControlsSO.Jump();
            }

        }
        
        private void PlayerInputSO_OnPush(CallbackContext obj)
        {
            if (playerScript.UILocked) return;
            if (obj.started)
            {
                PlayerControlsSO.Push();
            }
        }
        
        private void PlayerInputSO_OnThrow(CallbackContext obj)
        {
            if (playerScript.UILocked) return;
            if (obj.started){
                PlayerControlsSO.Throw();
            }else if(obj.canceled){
                PlayerControlsSO.ThrowCancel();
            }
        }
        
        private void PlayerInputSO_OnRoll(CallbackContext obj)
        {
            if (playerScript.UILocked) return;
            if(obj.performed)
                PlayerControlsSO.Roll();
        }

        private void PlayerInputSO_OnMenuCelular(CallbackContext obj)
        {
            if (obj.started)
            {
                PlayerControlsSO.MenuCelular();
            }
        }
       
        private void PlayerInputSOOnOnDebug(CallbackContext obj)
        {
            if (playerScript.UILocked) return;
            if (obj.performed == true){
                PlayerControlsSO.Debug();
            }
        }
        
        private void PlayerInputSO_OnScroll(CallbackContext obj)
        {
            if (!playerScript.isLocalPlayer) return;
            if (!playerScript.panel) return; // Só funciona quando está no painel
            
            Vector2 scrollValue = obj.ReadValue<Vector2>();
            PlayerControlsSO.ZoomPanel(scrollValue.y);
        }
        
        private void PlayerInputSO_OnCancel(CallbackContext obj)
        {
            // Usa started para evitar múltiplos disparos em sessões com vários jogadores
            if (!playerScript.isLocalPlayer) return;
            if (!obj.started) return;
            PlayerControlsSO.ClosePanel();
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
