﻿using pdxpartyparrot.Core.Actors;
using pdxpartyparrot.Game.Data;

using UnityEngine;

namespace pdxpartyparrot.Game.Actors
{
    public class ThirdPersonController : ActorController
    {
        public ThirdPersonControllerData ControllerData { get; set; }

#region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            InitRigidbody();
        }
#endregion

        private void InitRigidbody()
        {
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
            Rigidbody.freezeRotation = true;
            Rigidbody.detectCollisions = true;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // we run the follow cam in FixedUpdate() and interpolation interferes with that
            Rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        public override void RotateModel(Vector3 axes, float dt)
        {
            if(!Owner.CanMove) {
                return;
            }
        }

        public override void Turn(Vector3 axes, float dt)
        {
            if(!Owner.CanMove) {
                return;
            }
        }

        public override void Move(Vector3 axes, float dt)
        {
            if(!Owner.CanMove) {
                return;
            }

            Owner.GameObject.transform.position += axes * ControllerData.MoveSpeed * dt;
        }
    }
}
