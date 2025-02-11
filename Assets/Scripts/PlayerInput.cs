using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

    public class PlayerInput : MonoBehaviour{
        
        public PlayerControlSO PlayerControlSO;

        private float _rawH, _rawV;
        private float _h, _v;
        private void Start(){
            PlayerControlSO.EventMove += EventMove;
        }
        private void OnDestroy(){
            PlayerControlSO.EventMove -= EventMove;
        }
        private void Update(){
            if(_rawH != 0)
                _h = Mathf.MoveTowards(_h, _rawH, PlayerControlSO.inputAcel * Time.deltaTime);
            else
                _h = Mathf.MoveTowards(_h, 0, PlayerControlSO.inputGravity * Time.deltaTime);
            
            if(_rawV != 0)
                _v = Mathf.MoveTowards(_v, _rawV, PlayerControlSO.inputAcel * Time.deltaTime);
            else
                _v = Mathf.MoveTowards(_v, 0, PlayerControlSO.inputGravity * Time.deltaTime);
            
            PlayerControlSO.OnCustomMove(new Vector2(_h, _v));
        }
        private void EventMove(Vector2 obj){
            _rawH = obj.x;
            _rawV = obj.y;
        }
    }
