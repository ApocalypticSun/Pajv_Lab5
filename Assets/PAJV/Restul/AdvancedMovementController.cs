using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class AdvancedMovementController : MovementController
    {
        public bool IsSlamming { get { return _isSlamming; } }
        public bool IsDashing { get { return _isDashing; } }


        [Header("Slamming")]
        [SerializeField]
        private float _slamDownForce = 50f;
        private bool _isSlamming;

        [Header("Dashing")]
        [SerializeField]
        private float _dashForce = 15f;
        private bool _isDashing;

        protected override void FirstInitialize()
        {
            _shouldCheckGround = true;

            base.FirstInitialize();
        }

        private void OnEnable(){
            Movement();
        }

        private void OnDisable(){
            UnMovement();
        }

        protected override void FixedUpdate()
        {   
            if(_isSlamming && _isGrounded){
                _isSlamming = false;
            }
            base.FixedUpdate();
        }

        
        private void Movement(){
            AdvancedMovementControls controls = (AdvancedMovementControls)_movementControls;
            controls.SlamButtonPressed += OnSlamButtonPressed;
            controls.DashButtonPressed += OnDashButtonPressed;
        }

        private void OnSlamButtonPressed(){
            if(!_isGrounded){
                _isSlamming = true;
                _rigidBody.AddForce(new Vector3(0f, -_slamDownForce, 0f), ForceMode.Impulse);
            }
        }

        private void OnDashButtonPressed(){
            _isDashing = true;

            _rigidBody.AddForce(new Vector3(
                    newDir.x,
                    0f,
                    newDir.y) * _dashForce, ForceMode.Impulse);
            _isDashing = false;
        }

        private void UnMovement(){
            AdvancedMovementControls controls = (AdvancedMovementControls)_movementControls;
            controls.SlamButtonPressed -= OnSlamButtonPressed;
            controls.DashButtonPressed -= OnDashButtonPressed;
        }
       
    }

