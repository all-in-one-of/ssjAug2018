﻿using System.Collections;

using JetBrains.Annotations;

using pdxpartyparrot.Core.Audio;
using pdxpartyparrot.Core.Camera;
using pdxpartyparrot.Core.Network;
using pdxpartyparrot.Core.Util;
using pdxpartyparrot.ssjAug2018.Camera;
using pdxpartyparrot.ssjAug2018.GameState;
using pdxpartyparrot.ssjAug2018.Items;
using pdxpartyparrot.ssjAug2018.UI;
using pdxpartyparrot.ssjAug2018.World;

using UnityEngine;
using UnityEngine.Networking;

namespace pdxpartyparrot.ssjAug2018.Players
{
    [RequireComponent(typeof(FollowTarget))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class Player : NetworkActor
    {
#region Inventory
        [Header("Inventory")]

        [SerializeField]
        [ReadOnly]
        [SyncVar]
        private int _currentLetterCount;

        public int CurrentLetterCount => _currentLetterCount;

        public bool CanThrowMail => CurrentLetterCount > 0;

        [SerializeField]
        [ReadOnly]
        [SyncVar]
        private long _reloadCompleteTime;

        public bool IsReloading => _reloadCompleteTime > 0 && TimeManager.Instance.CurrentUnixMs <= _reloadCompleteTime;
#endregion

        [SerializeField]
        [ReadOnly]
        [SyncVar]
        private int _score;

        public int Score => _score;

        [SerializeField]
        [ReadOnly]
        [SyncVar]
        private bool _isDead;

        public bool IsDead => _isDead;

        public FollowTarget FollowTarget { get; private set; }

        public PlayerController PlayerController => (PlayerController)Controller;

        public CapsuleCollider CapsuleCollider => (CapsuleCollider)Collider;

        private AudioSource _audioSource;

        [CanBeNull]
        private Camera.Viewer _viewer;

        [CanBeNull]
        public override Core.Camera.Viewer Viewer => _viewer;

#region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            NetworkIdentity.localPlayerAuthority = true;
            NetworkTransform.transformSyncMode = NetworkTransform.TransformSyncMode.SyncRigidbody3D;
            NetworkTransform.syncRotationAxis = NetworkTransform.AxisSyncMode.AxisY;

#if UNITY_EDITOR
            if(!(Controller is PlayerController)) {
                Debug.LogError("Player controller must be a PlayerController!");
            }
#endif

            FollowTarget = GetComponent<FollowTarget>();

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            AudioManager.Instance.InitSFXAudioMixerGroup(_audioSource);

            PlayerManager.Instance.Register(this);
        }

        protected override void OnDestroy()
        {
            if(ViewerManager.HasInstance) {
                ViewerManager.Instance.ReleaseViewer(_viewer);
            }
            _viewer = null;

            if(PlayerManager.HasInstance) {
                PlayerManager.Instance.Unregister(this);
            }

            base.OnDestroy();
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckMailboxTrigger(other.gameObject);
        }
#endregion

        private bool Initialize()
        {
            InitializeLocalPlayer();
            PlayerController.Initialize(this);

            // TODO: encapsulate this somewhere better
            PlayerController.Rigidbody.mass = PlayerManager.Instance.PlayerData.Mass;
            PlayerController.Rigidbody.drag = PlayerManager.Instance.PlayerData.Drag;
            PlayerController.Rigidbody.angularDrag = PlayerManager.Instance.PlayerData.AngularDrag;

            _currentLetterCount = PlayerManager.Instance.PlayerData.MaxLetters;

            return true;
        }

        [Client]
        private void InitializeLocalPlayer()
        {
            _viewer = (Camera.Viewer)ViewerManager.Instance.AcquireViewer();
            if(null != _viewer) {
                _viewer.Initialize(this);
            }

            UIManager.Instance.InitializePlayerUI(this);
            if(null != UIManager.Instance.PlayerUI) {
                UIManager.Instance.PlayerUI.PlayerHUD.ShowInfoText();
            }
        }

        [Server]
        private void CheckMailboxTrigger(GameObject go)
        {
            Mailbox mailbox = go.GetComponent<Mailbox>();
            if(null == mailbox) {
                return;
            }

            int consumed = mailbox.PlayerCollide(this);
            if(consumed < 1) {
                return;
            }

            _currentLetterCount -= consumed;
            CheckReload();
        }

        [Server]
        private void CheckReload()
        {
            if(_currentLetterCount > 0) {
                return;
            }

            _currentLetterCount = 0;
            Reload();
        }

        [Server]
        private void Reload()
        {
            _reloadCompleteTime = TimeManager.Instance.CurrentUnixMs + PlayerManager.Instance.PlayerData.ReloadTimeMs;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            yield return new WaitForSeconds(PlayerManager.Instance.PlayerData.ReloadTimeSeconds);

            _currentLetterCount = PlayerManager.Instance.PlayerData.MaxLetters;
        }

        [Server]
        public void IncreaseScore(int amount)
        {
            _score += amount;
        }

        [Server]
        public void Kill()
        {
            if(IsDead) {
                return;
            }

            _isDead = true;

            RpcDead();

            StartCoroutine(RespawnRoutine());
        }

        [Server]
        private IEnumerator RespawnRoutine()
        {
            Debug.Log($"Player respawning in {GameStateManager.Instance.GameData.PlayerRespawnSeconds} seconds");
            yield return new WaitForSeconds(GameStateManager.Instance.GameData.PlayerRespawnSeconds);

            PlayerManager.Instance.RespawnPlayer(this);
        }

#region Commands
        [Command]
        public void CmdThrowMail(Vector3 origin, Vector3 direction, float speed)
        {
            if(!CanThrowMail) {
                return;
            }

            Mail mail = ItemManager.Instance.GetMail();
            Vector3 velocity = direction * speed;
            if(null != mail) {
                mail.Throw(this, origin, velocity);
            }

            _currentLetterCount--;
            CheckReload();
        }

        [Command]
        public void CmdThrowSnowball(Vector3 origin, Vector3 direction, float speed)
        {
            Debug.Log("TODO: throw a snowball!");
        }
#endregion

#region Callbacks
        [ClientRpc]
        private void RpcDead()
        {
            //Animator.SetBool(PlayerManager.Instance.PlayerData.DeadParam, true);

            Debug.Log("TODO: show player dead UI on client");
        }

        public override void OnSpawn()
        {
            Debug.Log($"Spawning player (isLocalPlayer={isLocalPlayer})");

            Initialize();

            _isDead = false;
        }

        public void OnRespawn()
        {
            Debug.Log($"Respawning player (isLocalPlayer={isLocalPlayer})");

            _isDead = false;

            //Animator.SetBool(PlayerManager.Instance.PlayerData.DeadParam, false);
        }
#endregion
    }
}
