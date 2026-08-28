using Assets.Scripts.Areas.Shared.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 Move => InputFocusUI.IsAnyInputFocused ? Vector2.zero : _move;
        public Vector2 Look => InputFocusUI.IsAnyInputFocused ? Vector2.zero : _look;
        public bool Jump => !InputFocusUI.IsAnyInputFocused && _jump;
        public bool Sprint => !InputFocusUI.IsAnyInputFocused && _sprint;
        public bool Rotate => !InputFocusUI.IsAnyInputFocused && _rotate;

        private Vector2 _move;
        private Vector2 _look;
        private bool _jump;
        private bool _sprint;
        private bool _rotate;

        [Header("Movement Settings")]
        public bool AnalogMovement;

        [Header("Mouse Cursor Settings")]
        public bool CursorLocked = true;
        public bool CursorInputForLook = true;

        public void Start()
        {
            SprintInput(true);
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (CursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            //SprintInput(value.isPressed);
        }
#endif

        public void OnRotate(InputValue value)
        {
            _rotate = value.isPressed;
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            _move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            _look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            _jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            _sprint = newSprintState;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(CursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
