using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_Multi
{   // Script Class
    /// Provides the interface between the script and bot
    ///////////////////////////////////////////////////////
    public partial class Medic : Bot
    {
        private void fireMedkit(int now)
        {
            //Try to prevent double healing
            if (now - _lastHeal < 2000)
                return;

            //Subtract our energy cost
            _energy -= 120;

            //Heal the players in range
            IEnumerable<Player> players = _arena.getPlayersInRange(_state.positionX, _state.positionY, 200).Where(p => p._team == _team);
            foreach (Player player in players)
            {
                player.heal((Assets.ItemInfo.RepairItem)AssetManager.Manager.getItemByID(_type.InventoryItems[1]), _target);
            }

            //Heal ourself (this is hack)
            _state.health += 35;
            if (_state.health > 80)
                _state.health = 80;


            _lastHeal = now;
        }
    }
}
