using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

 public class AdvancedMovementControls : MovementControls
    {
        public event System.Action SlamButtonPressed;
        public event System.Action DashButtonPressed;

        private NewControls _gameControls;

        Vector2 currentMovement;
        private void Awake(){
            _gameControls = new NewControls();
            _gameControls.PlayerControls.Move.performed += context => 
            {
                currentMovement = context.ReadValue<Vector2>();
                //Debug.Log("Y: " + currentMovement);
            };
        }

        public override void Initialize()
        {
            Move();
        }

        private void Move(){
            // Movement Vector Input (WASD)
            _gameControls.PlayerControls.Move.performed += context => this.MoveDirection = context.ReadValue<Vector2>();
            _gameControls.PlayerControls.Move.canceled += context => this.MoveDirection = Vector2.zero;
            // Sprinting
            _gameControls.PlayerControls.Sprint.performed += context => this.OnStartSprint();
            _gameControls.PlayerControls.Sprint.canceled += context => this.OnStopSprint();

            // Jumping
            _gameControls.PlayerControls.Jump.performed += context => this.OnJump();

            // Slamming
            _gameControls.PlayerControls.Slam.performed += context => SlamButtonPressed?.Invoke();

            // Dashing
            _gameControls.PlayerControls.Dash.performed += context => DashButtonPressed?.Invoke();

        }

        private void OnEnable(){
            _gameControls.Enable();
        }

        private void OnDisable(){
            _gameControls.Disable();
        }
    }

