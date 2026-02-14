using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public class BasicMovementControls : MovementControls {

        public KeyCode JumpKeyCode = KeyCode.Space;
        public KeyCode SprintKeyCode = KeyCode.LeftShift;

        float _horizontal;
        float _vertical;

        public override void Initialize()
        {
            _horizontal = 0f;
            _vertical = 0f;
        }

        private void JumpInput(){
            if(Input.GetKeyDown(JumpKeyCode)){
                OnJump();
            }
        }

        private void SprintInput(){
            if(Input.GetKeyDown(SprintKeyCode)){
                OnStartSprint();
            }
            if(Input.GetKeyUp(SprintKeyCode)){
                OnStopSprint();
            }
        }

        private void MoveInput(){
            _horizontal = Input.GetAxisRaw("Horizontal");
            _vertical = Input.GetAxisRaw("Vertical");

            MoveDirection = new Vector2(_horizontal, _vertical);

        }

        private void Update(){
            MoveInput();
            SprintInput();
            JumpInput();
        }
    }