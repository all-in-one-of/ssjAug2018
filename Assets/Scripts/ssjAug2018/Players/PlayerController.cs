﻿using System;
using System.Collections;

using pdxpartyparrot.Core.DebugMenu;
using pdxpartyparrot.Core.Util;
using pdxpartyparrot.Game.Actors;
using pdxpartyparrot.ssjAug2018.Data;
using pdxpartyparrot.ssjAug2018.World;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace pdxpartyparrot.ssjAug2018.Players
{
    public sealed class PlayerController : ThirdPersonController
    {
        private enum MovementState
        {
            Platforming,
            Climbing,
            Hovering
        }

        [SerializeField]
        private PlayerControllerData _playerControllerData;

#region Movement State
        [Header("Movement State")]

        [SerializeField]
        [ReadOnly]
        private MovementState _movementState = MovementState.Platforming;

        public bool IsClimbing => MovementState.Climbing == _movementState;

        public bool IsGrabbing => IsClimbing;

        public bool IsHovering => MovementState.Hovering == _movementState;
#endregion

        [Space(10)]

#region Hands
        [Header("Hands")]

        [SerializeField]
        [FormerlySerializedAs("_leftGrabCheckTransform")]
        private Transform _leftHandTransform;

        private RaycastHit? _leftHandHitResult;

        private bool CanGrabLeft => null != _leftHandHitResult;

        [SerializeField]
        [FormerlySerializedAs("_rightGrabCheckTransform")]
        private Transform _rightHandTransform;

        private RaycastHit? _rightHandHitResult;

        private bool CanGrabRight => null != _rightHandHitResult;
#endregion

        [Space(10)]

#region Head
        [Header("Head")]

        [SerializeField]
        private Transform _headTransform;

        private RaycastHit? _headHitResult;
#endregion

        [Space(10)]

#region Chest
        [Header("Chest")]

        [SerializeField]
        private Transform _chestTransform;

        private RaycastHit? _chestHitResult;
#endregion

        private bool CanClimbUp => IsClimbing && (null == _headHitResult && null != _chestHitResult);

        [Space(10)]

#region Jumping
        [Header("Jumping")]

        [SerializeField]
        [ReadOnly]
        private bool _canJump;

        [SerializeField]
        [ReadOnly]
        private long _longJumpTriggerTime;

        private bool CanLongJump => _longJumpTriggerTime > 0 && TimeManager.Instance.CurrentUnixMs >= _longJumpTriggerTime;
#endregion

#region Hover
        [Header("Hover")]

        [SerializeField]
        [ReadOnly]
        private long _hoverTriggerTime;

        [SerializeField]
        [ReadOnly]
        private int _hoverTimeMs;

        [SerializeField]
        [ReadOnly]
        private long _hoverCooldownEndTime;

        public float HoverRemainingPercent => _hoverTimeMs / (float)_playerControllerData.HoverTimeMs;

        private bool IsHoverCooldown => TimeManager.Instance.CurrentUnixMs < _hoverCooldownEndTime;

        private bool CanHover => _hoverTriggerTime > 0 && TimeManager.Instance.CurrentUnixMs >= _hoverTriggerTime;
#endregion

#region Throwing
        [SerializeField]
        [ReadOnly]
        private bool _canThrow;

        [SerializeField]
        private long _autoThrowTriggerTime;

        private bool ShouldAutoThrow => _autoThrowTriggerTime > 0 && TimeManager.Instance.CurrentUnixMs >= _autoThrowTriggerTime;
#endregion

#region Aiming
        [SerializeField]
        [ReadOnly]
        private bool _isAiming;

        public bool IsAiming => _isAiming;
#endregion

        [SerializeField]
        private bool _breakOnFall;

        public Player Player => (Player)Owner;

        private DebugMenuNode _debugMenuNode;

#region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            InitDebugMenu();

            Debug.Assert(Math.Abs(_leftHandTransform.position.y - _rightHandTransform.position.y) < float.Epsilon, "Player hands are at different heights!");
            Debug.Assert(_headTransform.position.y > _leftHandTransform.position.y, "Player head should be above player hands!");
            Debug.Assert(_chestTransform.position.y < _leftHandTransform.position.y, "Player chest should be below player hands!");
        }

        private void OnDestroy()
        {
            DestroyDebugMenu();
        }

        protected override void Update()
        {
            base.Update();

            float dt = Time.deltaTime;

            UpdateJumping(dt);
            UpdateHovering(dt);
            UpdateThrowing(dt);
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

// TODO: encapsulate the math here so we a) don't duplicate it in the raycast methods and b) guarantee we always match the math done in the raycast methods

            // left hand
            Gizmos.color = null != _leftHandHitResult ? Color.red : Color.yellow;
            Gizmos.DrawLine(_leftHandTransform.position, _leftHandTransform.position + transform.forward * _playerControllerData.ArmRayLength);
            if(IsClimbing && !CanGrabLeft && CanGrabRight) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_leftHandTransform.position, _leftHandTransform.position + (Quaternion.AngleAxis(_playerControllerData.WrapAroundAngle, transform.up) * transform.forward) * _playerControllerData.ArmRayLength * 2.0f);
            }

            // right hand
            Gizmos.color = null != _rightHandHitResult ? Color.red : Color.yellow;
            Gizmos.DrawLine(_rightHandTransform.position, _rightHandTransform.position + transform.forward * _playerControllerData.ArmRayLength);
            if(IsClimbing && CanGrabLeft && !CanGrabRight) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_rightHandTransform.position, _rightHandTransform.position + (Quaternion.AngleAxis(-_playerControllerData.WrapAroundAngle, transform.up) * transform.forward) * _playerControllerData.ArmRayLength * 2.0f);
            }

            if(IsClimbing) {
                // head
                Gizmos.color = null != _headHitResult ? Color.red : Color.yellow;
                Gizmos.DrawLine(_headTransform.position, _headTransform.position + (Quaternion.AngleAxis(-_playerControllerData.HeadRayAngle, transform.right) * transform.forward) * _playerControllerData.HeadRayLength);

                if(!CanGrabLeft && !CanGrabRight && CanClimbUp) {
                    Gizmos.color = Color.white;
                    Vector3 start = _headTransform.position + (Quaternion.AngleAxis(-_playerControllerData.HeadRayAngle, transform.right) * transform.forward) * _playerControllerData.HeadRayLength;
                    Vector3 end = start + Player.CapsuleCollider.height * -Vector3.up;
                    Gizmos.DrawLine(start, end);
                }

                // chest
                Gizmos.color = null != _chestHitResult ? Color.red : Color.yellow;
                Gizmos.DrawLine(_chestTransform.position, _chestTransform.position + transform.forward * _playerControllerData.ChestRayLength);
            }

            // feet
/*
            if(!IsGrabbing && IsGrounded) {
                Gizmos.color = null != _footHitResults[0] ? Color.red : Color.yellow;
                Gizmos.DrawLine(_footTransform.position, _footTransform.position + (Quaternion.AngleAxis(0.0f, transform.up) * Quaternion.AngleAxis(_playerControllerData.FootRayAngle, transform.right) * transform.forward) * _playerControllerData.FootRayLength);
                Gizmos.color = null != _footHitResults[1] ? Color.red : Color.yellow;
                Gizmos.DrawLine(_footTransform.position, _footTransform.position + (Quaternion.AngleAxis(90.0f, transform.up) * Quaternion.AngleAxis(_playerControllerData.FootRayAngle, transform.right) * transform.forward) * _playerControllerData.FootRayLength);
                Gizmos.color = null != _footHitResults[2] ? Color.red : Color.yellow;
                Gizmos.DrawLine(_footTransform.position, _footTransform.position + (Quaternion.AngleAxis(180.0f, transform.up) * Quaternion.AngleAxis(_playerControllerData.FootRayAngle, transform.right) * transform.forward) * _playerControllerData.FootRayLength);
                Gizmos.color = null != _footHitResults[3] ? Color.red : Color.yellow;
                Gizmos.DrawLine(_footTransform.position, _footTransform.position + (Quaternion.AngleAxis(270.0f, transform.up) * Quaternion.AngleAxis(_playerControllerData.FootRayAngle, transform.right) * transform.forward) * _playerControllerData.FootRayLength);
            }
*/
        }
#endregion

        public void Initialize(Player player)
        {
            base.Initialize(player);

            StartCoroutine(RaycastRoutine());
        }

        public override void AnimationMove(Vector3 axes, float dt)
        {
            if(IsGrabbing) {
                return;
            }

            base.AnimationMove(axes, dt);
        }

        public override void PhysicsMove(Vector3 axes, float dt)
        {
            if(IsGrabbing) {
                Vector3 velocity = transform.localRotation * (axes * _playerControllerData.ClimbSpeed);
                if(IsGrounded && velocity.y < 0.0f) {
                    velocity.y = 0.0f;
                }
                Rigidbody.MovePosition(Rigidbody.position + velocity * dt);
            } else if(IsHovering) {
                Vector3 acceleration = (_playerControllerData.HoverAcceleration + ControllerData.FallSpeedAdjustment) * Vector3.up;
                Rigidbody.AddForce(acceleration, ForceMode.Acceleration);

                base.PhysicsMove(axes, dt);
            } else {
                base.PhysicsMove(axes, dt);
            }
        }

#region Actions
        public void Grab()
        {
            if(IsGrabbing || (!CanGrabLeft && !CanGrabRight)) {
                return;
            }

            EnableClimbing(true);

            if(null != _leftHandHitResult) {
                AttachToSurface(_leftHandHitResult.Value);
            } else if(null != _rightHandHitResult) {
                AttachToSurface(_rightHandHitResult.Value);
            }
        }

        public void Drop()
        {
            if(IsGrabbing) {
                DisableGrabbing();
            } else {
                CheckDropDown();
            }
        }

        public void StartThrow()
        {
            if(!Player.CanThrowMail) {
                return;
            }

            _canThrow = true;

            _autoThrowTriggerTime = TimeManager.Instance.CurrentUnixMs + _playerControllerData.AutoThrowMs;

            Player.Animator.SetBool(_playerControllerData.ThrowingMailParam, true);
        }

        public void Throw()
        {
            if(_canThrow) {
                DoThrow();
            }

            Player.Animator.SetBool(_playerControllerData.ThrowingMailParam, false);

            _canThrow = true;
        }

        private void DoThrow()
        {
            _autoThrowTriggerTime = 0;

            if(null == Player.Viewer) {
                Debug.LogWarning("Non-local player doing a throw!");
                return;
            }

            Player.CmdThrow(_rightHandTransform.position, Player.Viewer.transform.forward, _playerControllerData.ThrowSpeed);

            Player.Animator.SetTrigger(_playerControllerData.ThrowMailParam);
        }

        public void JumpStart()
        {
            _canJump = true;
            _longJumpTriggerTime = 0;

            if(IsGrounded || IsGrabbing) {
                _longJumpTriggerTime = TimeManager.Instance.CurrentUnixMs + _playerControllerData.LongJumpHoldMs;
            }
        }

        public override void Jump()
        {
            if(_canJump) {
                DisableGrabbing();

                base.Jump();
            }

            _longJumpTriggerTime = 0;
            _canJump = true;
        }

        public void HoverStart()
        {
            _hoverTriggerTime = 0;

            if(!IsGrabbing) {
                _hoverTriggerTime = TimeManager.Instance.CurrentUnixMs + _playerControllerData.HoverHoldMs;
            }
        }

        public void Hover()
        {
            EnableHovering(false);

            _hoverTriggerTime = 0;
            _canJump = true;
        }
#endregion

        private void DisableGrabbing()
        {
            EnableClimbing(false);
        }

        private void EnableClimbing(bool enable)
        {
            _movementState = enable ? MovementState.Climbing : MovementState.Platforming;
            Rigidbody.isKinematic = enable;

            Player.Animator.SetBool(_playerControllerData.ClimbingParam, enable);

            if(enable) {
                DoubleJumpCount = 0;
            }
        }

        public void UpdateJumping(float dt)
        {
            if(_canJump && CanLongJump) {
                DisableGrabbing();
                DoJump(_playerControllerData.LongJumpHeight, _playerControllerData.LongJumpParam);

                _canJump = false;
            }
        }

        private void EnableHovering(bool enable)
        {
            _movementState = enable ? MovementState.Hovering : MovementState.Platforming;

            if(enable) {
                _hoverTriggerTime = 0;
                Rigidbody.velocity = new Vector3(Rigidbody.velocity.x, 0.0f, Rigidbody.velocity.z);
            } else {
                _hoverCooldownEndTime = TimeManager.Instance.CurrentUnixMs + _playerControllerData.HoverCooldownMs;
            }
        }

        private void UpdateHovering(float dt)
        {
            int dtms = (int)(dt * 1000.0f);

            if(IsHovering) {
                if(_hoverTimeMs >= _playerControllerData.HoverTimeMs) {
                    _hoverTimeMs = _playerControllerData.HoverTimeMs;
                    EnableHovering(false);
                } else {
                    _hoverTimeMs += dtms;
                }
                _canJump = false;
            } else if(!IsHoverCooldown) {
                _hoverCooldownEndTime = 0;
                if(_hoverTimeMs > 0) {
                    _hoverTimeMs -= dtms;
                    if(_hoverTimeMs < 0) {
                        _hoverTimeMs = 0;
                    }
                }

                if(CanHover) {
                    EnableHovering(true);
                }
            }
        }

        private void UpdateThrowing(float dt)
        {
            if(ShouldAutoThrow) {
                DoThrow();
                _canThrow = false;
            }
        }

        private IEnumerator RaycastRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(RaycastRoutineRate);
            while(true) {
                UpdateRaycasts();

                HandleRaycasts();

                yield return wait;
            }
        }

        private void UpdateRaycasts()
        {
            Profiler.BeginSample("PlayerController.UpdateRaycasts");
            try {
                UpdateHandRaycasts();
                UpdateHeadRaycasts();
                UpdateChestRaycasts();
            } finally {
                Profiler.EndSample();
            }
        }

#region Hand Raycasts
        private void UpdateHandRaycasts()
        {
            UpdateLeftHandRaycasts();
            UpdateRightHandRaycasts();
        }

        private void UpdateLeftHandRaycasts()
        {
            _leftHandHitResult = null;

            RaycastHit hit;
            if(Physics.Raycast(_leftHandTransform.position, transform.forward, out hit, _playerControllerData.ArmRayLength, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                IGrabbable grabbable = hit.transform.GetComponent<IGrabbable>();
                if(null != grabbable) {
                    _leftHandHitResult = hit;
                }
            }
        }

        private void UpdateRightHandRaycasts()
        {
            _rightHandHitResult = null;

            RaycastHit hit;
            if(Physics.Raycast(_rightHandTransform.position, transform.forward, out hit, _playerControllerData.ArmRayLength, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                IGrabbable grabbable = hit.transform.GetComponent<IGrabbable>();
                if(null != grabbable) {
                    _rightHandHitResult = hit;
                }
            }
        }
#endregion

#region Head Raycasts
        private void UpdateHeadRaycasts()
        {
            if(!IsClimbing) {
                return;
            }

            _headHitResult = null;

            RaycastHit hit;
            if(Physics.Raycast(_headTransform.position, Quaternion.AngleAxis(-_playerControllerData.HeadRayAngle, transform.right) * transform.forward, out hit, _playerControllerData.HeadRayLength, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                IGrabbable grabbable = hit.transform.GetComponent<IGrabbable>();
                if(null != grabbable) {
                    _headHitResult = hit;
                }
            }
        }
#endregion

#region Chest Raycasts
        private void UpdateChestRaycasts()
        {
            if(!IsClimbing) {
                return;
            }

            _chestHitResult = null;

            RaycastHit hit;
            if(Physics.Raycast(_chestTransform.position, transform.forward, out hit, _playerControllerData.ChestRayLength, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                IGrabbable grabbable = hit.transform.GetComponent<IGrabbable>();
                if(null != grabbable) {
                    _chestHitResult = hit;
                }
            }
        }
#endregion

/*
#region Foot Raycasts
        private void UpdateFootRaycasts()
        {
            if(IsGrabbing || !IsGrounded) {
                return;
            }

            UpdateFootRaycast(0, 0.0f);
            UpdateFootRaycast(1, 90.0f);
            UpdateFootRaycast(2, 180.0f);
            UpdateFootRaycast(3, 270.0f);
        }

        private void UpdateFootRaycast(int idx, float angle)
        {
            _footHitResults[idx] = null;

            RaycastHit hit;
            if(Physics.Raycast(_footTransform.position, Quaternion.AngleAxis(angle, transform.up) * Quaternion.AngleAxis(_playerControllerData.FootRayAngle, transform.right) * transform.forward, out hit, _playerControllerData.FootRayLength, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                _footHitResults[idx] = hit;
            }
        }
#endregion
*/

        private void HandleRaycasts()
        {
            Profiler.BeginSample("PlayerController.HandleRaycasts");
            try {
                if(IsClimbing) {
                    HandleClimbingRaycasts();
                }
            } finally {
                Profiler.EndSample();
            }
        }

        private void HandleClimbingRaycasts()
        {
            if(!CanGrabLeft && CanGrabRight) {
                CheckRotateLeft();
            } else if(CanGrabLeft && !CanGrabRight) {
                CheckRotateRight();
            } else if(!CanGrabLeft && !CanGrabRight) {
                if(CanClimbUp) {
                    CheckClimbUp();
                } else {
                    Debug.LogWarning("Unexpectedly fell off!");
                    DisableGrabbing();

                    if(_breakOnFall) {
                        Debug.Break();
                    }
                }
            }
        }

#region Auto-Rotate/Climb
        private bool CheckRotateLeft()
        {
            if(null == _rightHandHitResult) {
                return false;
            }

            RaycastHit hit;
            if(!Physics.Raycast(_leftHandTransform.position, Quaternion.AngleAxis(_playerControllerData.WrapAroundAngle, transform.up) * transform.forward, out hit, _playerControllerData.ArmRayLength * 2.0f, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                return false;
            }

            IGrabbable grabbable = hit.transform.GetComponent<IGrabbable>();
            if(null == grabbable) {
                return false;
            }

            if(hit.normal == _rightHandHitResult.Value.normal) {
                return false;
            }

            AttachToSurface(hit);
            _leftHandHitResult = hit;

            Vector3 offset = (Player.CapsuleCollider.radius * 2.0f) * -transform.right;
            Rigidbody.position += offset;

            return true;
        }

        private bool CheckRotateRight()
        {
            if(null == _leftHandHitResult) {
                return false;
            }

            RaycastHit hit;
            if(!Physics.Raycast(_rightHandTransform.position, Quaternion.AngleAxis(-_playerControllerData.WrapAroundAngle, transform.up) * transform.forward, out hit, _playerControllerData.ArmRayLength * 2.0f, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                return false;
            }

            IGrabbable grabbable = hit.transform.GetComponent<IGrabbable>();
            if(null == grabbable) {
                return false;
            }

            if(hit.normal == _leftHandHitResult.Value.normal) {
                return false;
            }

            AttachToSurface(hit);
            _rightHandHitResult = hit;

            Vector3 offset = (Player.CapsuleCollider.radius * 2.0f) * transform.right;
            Rigidbody.position += offset;

            return true;

        }

        private bool CheckClimbUp()
        {
            // cast a ray from the end of our rotated head check straight down to see if we can stand here
            Vector3 start = _headTransform.position + (Quaternion.AngleAxis(-_playerControllerData.HeadRayAngle, transform.right) * transform.forward) * _playerControllerData.HeadRayLength;
            float length = Player.CapsuleCollider.height;

            RaycastHit hit;
            if(!Physics.Raycast(start, -Vector3.up, out hit, length, ControllerData.CollisionCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                return false;
            }

            ClimbUp(hit);

            return true;
        }

        private bool CheckDropDown()
        {
Debug.Log("TODO: check drop down");
            return false;
        }
#endregion

// TODO: smooth/animate these things

        private void AttachToSurface(RaycastHit hit)
        {
            // align to the surface
            transform.forward = -hit.normal;

            // keep a set distance away from the surface
            Vector3 targetPoint = hit.point + (hit.normal * _playerControllerData.AttachDistance);
            Vector3 a = targetPoint - Rigidbody.position;
            Vector3 p = Vector3.Project(a, hit.normal);
            Vector3 offset = Player.CapsuleCollider.radius * hit.normal;
            Rigidbody.position += p + offset;
        }

        private void ClimbUp(RaycastHit hit)
        {
            Vector3 targetPoint = hit.point + (hit.normal * _playerControllerData.AttachDistance);
            Vector3 a = targetPoint - Rigidbody.position;
            Vector3 p = Vector3.Project(a, hit.normal);
            Vector3 offset = (Player.CapsuleCollider.radius * 2.0f) * transform.forward;
            Rigidbody.position += p + offset;

            DisableGrabbing();
        }

        private void DropDown()
        {
Debug.Log("TODO: drop down");
        }

        private void InitDebugMenu()
        {
            _debugMenuNode = DebugMenuManager.Instance.AddNode(() => $"Player {Player.name} Controller");
            _debugMenuNode.RenderContentsAction = () => {
                _breakOnFall = GUILayout.Toggle(_breakOnFall, "Break on fall");
            };
        }

        private void DestroyDebugMenu()
        {
            if(DebugMenuManager.HasInstance) {
                DebugMenuManager.Instance.RemoveNode(_debugMenuNode);
            }
            _debugMenuNode = null;
        }
    }
}
