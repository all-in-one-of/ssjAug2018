﻿using pdxpartyparrot.Core.Util;

using UnityEngine;
using UnityEngine.Networking;

namespace pdxpartyparrot.Core.Actors
{
    [RequireComponent(typeof(ActorController))]
    public abstract class Actor : NetworkBehaviour, IActor
    {
        [SerializeField]
        [ReadOnly]
        private int _id = -1;

        public int Id => _id;

        [SerializeField]
        private ActorController _controller;

        public ActorController Controller => _controller;

        public virtual void Initialize(int id)
        {
            _id = id;

            _controller.Initialize(this);
        }
    }
}
