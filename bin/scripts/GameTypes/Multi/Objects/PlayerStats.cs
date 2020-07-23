using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    /// <summary>
    /// Stores our player information
    /// </summary>
    public class Stats
    { 
        public Player player { get; set; }
        public Team team { get; set; }
        public string teamname { get; set; }
        public string alias { get; set; }
        public long points { get; set; }
        public int kills { get; set; }
        public int deaths { get; set; }
        public int killPoints { get; set; }
        public int assistPoints { get; set; }
        public int bonusPoints { get; set; }
        public int titanPlaySeconds { get; set; }
        public int collectivePlaySeconds { get; set; }
        public bool hasPlayed { get; set; }

        public int flagCaptures { get; set; }

        //Kill stats
        public ItemInfo.Projectile lastUsedWep { get; set; }
        public int lastUsedWepKillCount { get; set; }
        public long lastUsedWepTick { get; set; }
        public int lastKillerCount { get; set; }

        //Medic stats
        public int potentialHealthHealed { get; set; }

        public bool onPlayingField { get; set; }
    }
}
