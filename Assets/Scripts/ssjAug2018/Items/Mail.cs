﻿using pdxpartyparrot.Core.Util;
using pdxpartyparrot.Core.Util.ObjectPool;

using UnityEngine;
using UnityEngine.Networking;

namespace pdxpartyparrot.ssjAug2018.Items
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PooledObject))]
    public sealed class Mail : NetworkBehaviour
    {
        [SerializeField]
        [ReadOnly]
        private long _despawnTime;

        private Rigidbody _rigidbody;
        private PooledObject _pooledObject;

#region Unity Lifecycle
        private void Awake()
        {
            NetworkTransform networkTransform = GetComponent<NetworkTransform>();
            networkTransform.transformSyncMode = NetworkTransform.TransformSyncMode.SyncRigidbody3D;
            networkTransform.syncRotationAxis = NetworkTransform.AxisSyncMode.AxisXYZ;

            _rigidbody = GetComponent<Rigidbody>();
            _pooledObject = GetComponent<PooledObject>();

            InitRigidbody();
        }

        private void Update()
        {
            if(_despawnTime > 0 && TimeManager.Instance.CurrentUnixMs >= _despawnTime) {
                RpcMiss();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            RpcHit();
        }
#endregion

        private void InitRigidbody()
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = ItemManager.Instance.ItemData.ThrownItemsUseGravity;
            _rigidbody.constraints = RigidbodyConstraints.None;
            _rigidbody.detectCollisions = true;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        public void Throw(Vector3 origin, Vector3 direction, float speed)
        {
            _rigidbody.position = origin;
            _rigidbody.velocity = direction * speed;
            _despawnTime = TimeManager.Instance.CurrentUnixMs + ItemManager.Instance.ItemData.MailDespawnMs;
        }

        [ClientRpc]
        private void RpcHit()
        {
Debug.Log("hit!");
            _pooledObject.Recycle();
        }

        [ClientRpc]
        private void RpcMiss()
        {
Debug.Log("miss!");
            _pooledObject.Recycle();
        }
    }
}
