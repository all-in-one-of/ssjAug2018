﻿using pdxpartyparrot.Core.Input;
using pdxpartyparrot.Game.Actors;

using UnityEngine;
using UnityEngine.Experimental.Input;

namespace pdxpartyparrot.ssjAug2018.Players
{
    public sealed class PlayerDriver : GamepadDriver
    {
        public new Player Owner => (Player)base.Owner;

#region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            InputManager.Instance.Controls.game.pause.performed += OnPause;
            InputManager.Instance.Controls.game.move.performed += OnMove;
            InputManager.Instance.Controls.game.look.performed += OnLook;
            InputManager.Instance.Controls.game.jump.performed += OnJump;
        }

        private void OnDestroy()
        {
            if(InputManager.HasInstance) {
                InputManager.Instance.Controls.game.pause.performed -= OnPause;
                InputManager.Instance.Controls.game.move.performed -= OnMove;
                InputManager.Instance.Controls.game.look.performed -= OnLook;
                InputManager.Instance.Controls.game.jump.performed -= OnJump;
            }
        }
#endregion

#region Event Handlers
        private void OnPause(InputAction.CallbackContext ctx)
        {
            if(!IsOurGamepad(ctx)) {
                return;
            }

Debug.Log("pause");
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            if(!IsOurGamepad(ctx)) {
                return;
            }

            Vector2 axes = ctx.ReadValue<Vector2>();
            LastMoveAxes = new Vector3(axes.x, 0.0f, axes.y);
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            if(!IsOurGamepad(ctx)) {
                return;
            }

Debug.Log("look");
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if(!IsOurGamepad(ctx)) {
                return;
            }

            Owner.PlayerController.Jump();
        }
#endregion
    }
}
