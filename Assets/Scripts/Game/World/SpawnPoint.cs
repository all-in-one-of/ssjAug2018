﻿using pdxpartyparrot.Core.Actors;

using UnityEngine;

namespace pdxpartyparrot.Game.World
{
    public class SpawnPoint : MonoBehaviour
    {
#region Unity Lifecycle
        protected virtual void Awake()
        {
            SpawnManager.Instance.RegisterSpawnPoint(this);
        }

        protected virtual void OnDestroy()
        {
            if(SpawnManager.HasInstance) {
                SpawnManager.Instance.UnregisterSpawnPoint(this);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
#endregion

        public virtual void Spawn(IActor actor)
        {
            actor.GameObject.transform.position = transform.position;
            actor.GameObject.transform.rotation = transform.rotation;

            actor.GameObject.SetActive(true);
        }
    }
}
