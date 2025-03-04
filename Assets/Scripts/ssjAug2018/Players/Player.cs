﻿using JetBrains.Annotations;

using pdxpartyparrot.Core.Actors;
using pdxpartyparrot.Core.Audio;
using pdxpartyparrot.Core.Camera;
using pdxpartyparrot.ssjAug2018.Camera;
using pdxpartyparrot.ssjAug2018.UI;

using UnityEngine;
using UnityEngine.Networking;

namespace pdxpartyparrot.ssjAug2018.Players
{
    [RequireComponent(typeof(NetworkPlayer))]
    [RequireComponent(typeof(FollowTarget))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(WeatherZoneEffect))]
    public sealed class Player : Actor
    {
        public override float Height => PlayerController.Capsule.height;

        public override float Radius => PlayerController.Capsule.radius;

        public override bool IsLocalActor => NetworkPlayer.isLocalPlayer;

        [SerializeField]
        private NetworkPlayer _networkPlayer;

        [SerializeField]
        private PlayerDriver _driver;

        public NetworkPlayer NetworkPlayer => _networkPlayer;

        public FollowTarget FollowTarget { get; private set; }

        public PlayerController PlayerController => (PlayerController)Controller;

        private AudioSource _audioSource;

        [CanBeNull]
        private Camera.Viewer _viewer;

        [CanBeNull]
        public override Core.Camera.Viewer Viewer => _viewer;

        public Camera.Viewer PlayerViewer => _viewer;

        private WeatherZoneEffect _weatherZoneEffect;

#region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            if(!(Controller is PlayerController)) {
                Debug.LogError("Player controller must be a PlayerController!");
            }
#endif

            FollowTarget = GetComponent<FollowTarget>();

            _audioSource = GetComponent<AudioSource>();
            AudioManager.Instance.InitSFXAudioMixerGroup(_audioSource);

            _weatherZoneEffect = GetComponent<WeatherZoneEffect>();

            PlayerManager.Instance.Register(this);
        }

        protected override void OnDestroy()
        {
            _weatherZoneEffect.ParticleSystemParent = null;

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
            if(NetworkServer.active) {
                NetworkPlayer.CheckMailboxTrigger(other.gameObject);
            }
        }
#endregion

        public override void Initialize(int id)
        {
            base.Initialize(id);

            InitializeLocalPlayer();
        }

        private void InitializeLocalPlayer()
        {
            if(!IsLocalActor) {
                return;
            }

            Debug.Log("Initializing local player");

            _driver.Initialize();

            _viewer = ViewerManager.Instance.AcquireViewer<Camera.Viewer>();
            if(null != _viewer) {
                _viewer.Initialize(this);
                _weatherZoneEffect.ParticleSystemParent = _viewer.transform;
            }

            UIManager.Instance.InitializePlayerUI(this);
            if(null != UIManager.Instance.PlayerUI) {
                UIManager.Instance.PlayerUI.PlayerHUD.ShowInfoText();
            }
        }

        public override void OnSpawn()
        {
            Debug.Log($"Spawning player (isLocalPlayer={IsLocalActor})");

            if(NetworkServer.active) {
                NetworkPlayer.Reset();
            }

            Initialize(NetworkPlayer.playerControllerId);
            if(!NetworkClient.active) {
                NetworkPlayer.RpcSpawn(NetworkPlayer.playerControllerId);
            }
        }

        public override void OnReSpawn()
        {
            Debug.Log($"Respawning player (isLocalPlayer={IsLocalActor})");

            if(NetworkServer.active) {
                NetworkPlayer.Reset();
            }

            Animator.SetBool(PlayerManager.Instance.PlayerData.DeadParam, false);
        }
    }
}
