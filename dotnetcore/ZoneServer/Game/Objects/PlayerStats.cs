using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Logic;

using Assets;


namespace InfServer.Game
{
    // Player Class
    /// Represents a single player in the server
    ///////////////////////////////////////////////////////
    public partial class Player : IClient, ILocatable
    {	// Member variables
        ///////////////////////////////////////////////////
        private Data.PlayerStats _stats;			//The player's total statistics
        private Data.PlayerStats _statsSession;		//The player's total statistics
        private Data.PlayerStats _statsGame;		//The player's statistics for the current game
        private Data.PlayerStats _statsLastGame;	//The player's statistics for the last game


        ///////////////////////////////////////////////////
        // Accessors
        ///////////////////////////////////////////////////
        #region Stat Accessors
        /// <summary>
        /// Returns the player's statistics
        /// </summary>
        public Data.PlayerStats StatsTotal
        {
            get
            {
                return _stats;
            }
        }

        /// <summary>
        /// Returns the player's statistics for the current session
        /// </summary>
        public Data.PlayerStats StatsCurrentSession
        {
            get
            {
                return _statsSession;
            }
        }

        /// <summary>
        /// Returns or sets the player's statistics for the current game
        /// NOTE: We only set our current game if it hasnt ended yet
        /// </summary>
        public Data.PlayerStats StatsCurrentGame
        {
            get
            {
                return _statsGame;
            }
            set
            {
                if (_statsGame != null)
                    _statsGame = value;
            }
        }

        /// <summary>
        /// Returns the player's statistics for the last game
        /// </summary>
        public Data.PlayerStats StatsLastGame
        {
            get
            {
                return _statsLastGame;
            }
        }

        /// <summary>
        /// Wipes all of the players total statistics
        /// </summary>
        public void WipeStats()
        {
            _stats = new Data.PlayerStats();
        }

        private static int ClampStat(long value)
        {
            if (value <= 0)
                return 0;

            if (value >= int.MaxValue)
                return int.MaxValue;

            return (int)value;
        }

        private static int SetStat(ref int field, int value, bool allowDecrease)
        {
            int normalized = value;

            if (normalized < 0)
            {
                normalized = !allowDecrease && field >= 0 ? int.MaxValue : 0;
            }
            else if (!allowDecrease && normalized < field)
            {
                normalized = field;
            }

            int diff = normalized - field;
            field = normalized;
            return diff;
        }

        private static void AddStat(ref int field, int diff)
        {
            field = ClampStat((long)field + diff);
        }

        private void AddCurrentGameStat(Action<Data.PlayerStats> add)
        {
            if (_arena != null && _arena._currentGameStats.ContainsKey(_alias))
                add(_arena._currentGameStats[_alias]);
        }

        /// <summary>
        /// The player's cash amount
        /// </summary>
        public int Cash
        {
            get
            {
                return _stats.cash;
            }

            set
            {
                int diff = SetStat(ref _stats.cash, value, true);

                if (_statsSession != null)
                    AddStat(ref _statsSession.cash, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.cash, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.cash, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public long Points
        {
            get
            {
                return _stats.Points;
            }
        }

        /// <summary>
        /// The player's amount of experience remaining to be spent
        /// </summary>
        public int Experience
        {
            get
            {
                return _stats.experience;
            }

            set
            {
                int diff = SetStat(ref _stats.experience, value, true);
                if (diff > 0)
                    AddStat(ref _stats.experienceTotal, diff);

                if (_statsSession != null)
                {
                    AddStat(ref _statsSession.experience, diff);
                    if (diff > 0)
                        AddStat(ref _statsSession.experienceTotal, diff);
                }

                if (_statsGame != null)
                {
                    AddStat(ref _statsGame.experience, diff);
                    if (diff > 0)
                        AddStat(ref _statsGame.experienceTotal, diff);
                }

                AddCurrentGameStat(stats =>
                {
                    AddStat(ref stats.experience, diff);
                    if (diff > 0)
                        AddStat(ref stats.experienceTotal, diff);
                });
            }
        }

        /// <summary>
        /// The player's experience amount
        /// </summary>
        public int ExperienceTotal
        {
            get
            {
                return _stats.experienceTotal;
            }

            set
            {
                int diff = SetStat(ref _stats.experienceTotal, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.experienceTotal, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.experienceTotal, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.experienceTotal, diff));
            }
        }

        /// <summary>
        /// The amount of kills the player has made
        /// </summary>
        public int Kills
        {
            get
            {
                return _stats.kills;
            }

            set
            {
                int diff = SetStat(ref _stats.kills, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.kills, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.kills, diff);

                //Update our team stats
                if (_team != null)
                    _team._currentGameKills = ClampStat((long)_team._currentGameKills + diff);

                AddCurrentGameStat(stats => AddStat(ref stats.kills, diff));
            }
        }

        /// <summary>
        /// The amount of deaths the player has suffered
        /// </summary>
        public int Deaths
        {
            get
            {
                return _stats.deaths;
            }

            set
            {
                int diff = SetStat(ref _stats.deaths, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.deaths, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.deaths, diff);

                //Update our team stats
                if (_team != null)
                    _team._currentGameDeaths = ClampStat((long)_team._currentGameDeaths + diff);

                AddCurrentGameStat(stats => AddStat(ref stats.deaths, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int KillPoints
        {
            get
            {
                return _stats.killPoints;
            }

            set
            {
                int diff = SetStat(ref _stats.killPoints, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.killPoints, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.killPoints, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.killPoints, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int DeathPoints
        {
            get
            {
                return _stats.deathPoints;
            }

            set
            {
                int diff = SetStat(ref _stats.deathPoints, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.deathPoints, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.deathPoints, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.deathPoints, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int BonusPoints
        {
            get
            {
                return _stats.bonusPoints;
            }

            set
            {
                int diff = SetStat(ref _stats.bonusPoints, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.bonusPoints, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.bonusPoints, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.bonusPoints, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int AssistPoints
        {
            get
            {
                return _stats.assistPoints;
            }

            set
            {
                int diff = SetStat(ref _stats.assistPoints, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.assistPoints, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.assistPoints, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.assistPoints, diff));
            }
        }

        /// <summary>
        /// The amount of vehicle kills the player has made
        /// </summary>
        public int vehicleKills
        {
            get
            {
                return _stats.vehicleKills;
            }

            set
            {
                int diff = SetStat(ref _stats.vehicleKills, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.vehicleKills, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.vehicleKills, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.vehicleKills, diff));
            }
        }

        /// <summary>
        /// The amount of vehicle deaths the player has suffered
        /// </summary>
        public int vehicleDeaths
        {
            get
            {
                return _stats.vehicleDeaths;
            }

            set
            {
                int diff = SetStat(ref _stats.vehicleDeaths, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.vehicleDeaths, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.vehicleDeaths, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.vehicleDeaths, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int PlaySeconds
        {
            get
            {
                return _stats.playSeconds;
            }

            set
            {
                int diff = SetStat(ref _stats.playSeconds, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.playSeconds, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.playSeconds, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.playSeconds, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat1
        {
            get
            {
                return _stats.zonestat1;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat1, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat1, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat1, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat1, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat2
        {
            get
            {
                return _stats.zonestat2;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat2, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat2, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat2, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat2, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat3
        {
            get
            {
                return _stats.zonestat3;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat3, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat3, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat3, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat3, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat4
        {
            get
            {
                return _stats.zonestat4;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat4, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat4, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat4, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat4, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat5
        {
            get
            {
                return _stats.zonestat5;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat5, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat5, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat5, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat5, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat6
        {
            get
            {
                return _stats.zonestat6;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat6, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat6, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat6, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat6, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat7
        {
            get
            {
                return _stats.zonestat7;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat7, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat7, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat7, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat7, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat8
        {
            get
            {
                return _stats.zonestat8;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat8, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat8, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat8, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat8, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat9
        {
            get
            {
                return _stats.zonestat9;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat9, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat9, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat9, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat9, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat10
        {
            get
            {
                return _stats.zonestat10;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat10, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat10, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat10, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat10, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat11
        {
            get
            {
                return _stats.zonestat11;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat11, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat11, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat11, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat11, diff));
            }
        }

        /// <summary>
        /// The player's point amount
        /// </summary>
        public int ZoneStat12
        {
            get
            {
                return _stats.zonestat12;
            }

            set
            {
                int diff = SetStat(ref _stats.zonestat12, value, false);

                if (_statsSession != null)
                    AddStat(ref _statsSession.zonestat12, diff);

                if (_statsGame != null)
                    AddStat(ref _statsGame.zonestat12, diff);

                AddCurrentGameStat(stats => AddStat(ref stats.zonestat12, diff));
            }
        }
        #endregion

        ///////////////////////////////////////////////////
        // Member functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Causes the player's current game stats to be considered last game, and last game deprecated
        /// </summary>
        public void migrateStats()
        {
            _statsLastGame = (_statsGame == null) ? new Data.PlayerStats() : _statsGame;
            _statsGame = new Data.PlayerStats();
        }

        /// <summary>
        /// Stops all stats accumulated from this point on from counting
        /// </summary>
        public void suspendStats()
        {
            _suspStats = new Data.PlayerStats(_stats);

            _suspInventory = _inventory;
            _inventory = new Dictionary<int, InventoryItem>();

            foreach (InventoryItem ii in _suspInventory.Values)
            {
                InventoryItem nii = new InventoryItem();

                nii.item = ii.item;
                nii.quantity = ii.quantity;

                _inventory.Add(nii.item.id, nii);
            }

            _suspSkills = _skills;
            _skills = new Dictionary<int, SkillItem>();

            foreach (SkillItem si in _suspSkills.Values)
            {
                SkillItem nsi = new SkillItem();

                nsi.skill = si.skill;
                nsi.quantity = si.quantity;

                _skills.Add(nsi.skill.SkillId, nsi);
            }
        }

        /// <summary>
        /// Restores the suspended stats
        /// </summary>
        public void restoreStats()
        {	//Restore it all!
            //Sanity checks
            if (_suspStats == null)
                return;

            //Retrieve his stats
            _stats = _suspStats;
            _statsSession = new Data.PlayerStats();
            _statsGame = (_statsLastGame == null) ? new Data.PlayerStats() : _statsLastGame;
            _statsLastGame = null;

            _inventory = _suspInventory;
            _skills = _suspSkills;

            //Destroy suspended stats
            _suspStats = null;
            _suspInventory = null;
            _suspSkills = null;
        }

        /// <summary>
        /// Clears the player's stats for the current game
        /// </summary>
        public void clearCurrentStats()
        {
            _statsGame = new Data.PlayerStats();
        }
    }
}
