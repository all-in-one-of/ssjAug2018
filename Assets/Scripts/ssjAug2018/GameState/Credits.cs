﻿using pdxpartyparrot.Game.Menu;
using pdxpartyparrot.ssjAug2018.UI;

using UnityEngine;

namespace pdxpartyparrot.ssjAug2018.GameState
{
// TODO: this doesn't need to be a state, just build it into the main menu
    public sealed class Credits : pdxpartyparrot.Game.State.SubGameState
    {
        [SerializeField]
        private pdxpartyparrot.Game.Menu.Menu _menuPrefab;

        private pdxpartyparrot.Game.Menu.Menu _menu;

        public override void OnEnter()
        {
            base.OnEnter();

            _menu = Instantiate(_menuPrefab, UIManager.Instance.UIContainer.transform);
        }

        public override void OnExit()
        {
            Destroy(_menu.gameObject);
            _menu = null;

            base.OnExit();
        }
    }
}
