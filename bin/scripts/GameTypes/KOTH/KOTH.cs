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

//KING OF THE CORN!
namespace InfServer.Script.GameType_KOTH
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_KOTH : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config       

        private Team _victoryTeam;				//The team currently winning!
        private int _tickGameLastTickerUpdate;  //The tick at which the ticker was last updated
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        //Settings
        private int _minPlayers;				//The minimum amount of players
        private bool _firstGame = true;

        private class PlayerCrownStatus
        {
            public bool crown;                  //Player has crown?
            public int crownKills;              //Crown kills without a crown
            public int crownDeaths;             //Times died with a crown (counted until they lose it)
            public int expireTime;              //When the crown will expire
            public PlayerCrownStatus(bool bCrown)
            {
                crown = bCrown;
            }
            public PlayerCrownStatus()
            {
                crown = true;
            }
        }
        private Dictionary<Player, PlayerCrownStatus> _playerCrownStatus;
        private List<Player> _activeCrowns //List of people with a crown
        {
            get { return _playerCrownStatus.Where(p => p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Player> _noCrowns //List of people with no crowns
        {
            get { return _playerCrownStatus.Where(p => !p.Value.crown).Select(p => p.Key).ToList(); }
        }

        private List<Team> _crownTeams;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Performs script initialization
        /// </summary>
        public bool init(IEventObject invoker)
        {	//Populate our variables
            _arena = invoker as Arena;
            _config = _arena._server._zoneConfig;

            _minPlayers = _config.king.minimumPlayers;
            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            _crownTeams = new List<Team>();

            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;
            // List<Player> crowns = _activeCrowns;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Do we have enough players ingame?
            int playing = _arena.PlayerCount;

            //Check for people that are expiring
            if (_tickGameStart > 0)
            {
                foreach (var p in _playerCrownStatus)
                {
                    if ((now > p.Value.expireTime || _victoryTeam != null) && p.Value.crown)
                    {
                        p.Value.crown = false;
                        Helpers.Player_Crowns(_arena, true, _activeCrowns);
                        Helpers.Player_Crowns(_arena, false, _noCrowns);
                    }
                }

                //Get a list of teams with crowns and see if there is only one team
                _crownTeams.Clear();

                foreach (Player p in _activeCrowns)
                    if (!_crownTeams.Contains(p._team))
                        _crownTeams.Add(p._team);

                if (_crownTeams.Count == 1)
                {
                    _victoryTeam = _activeCrowns.First()._team;
                    _arena.sendArenaMessage(_victoryTeam._name + " is the winner");
                    gameVictory(_victoryTeam);
                    return true;
                }
            }

            //Check for a winner
            if (_tickGameStart > 0 && _activeCrowns.Count <= 1)
            {
                if (_activeCrowns.Count == 1)
                {
                    _victoryTeam = _activeCrowns.First()._team;
                    if (_victoryTeam != null && !_victoryTeam.IsSpec) //Check for team activity
                    {
                        //End the game
                        _arena.sendArenaMessage(_victoryTeam._name + " is the winner");
                        gameVictory(_victoryTeam);
                        return true;
                    }
                }
                else //still bugged, no winner
                {//All our crowners expired at the same time
                    _arena.sendArenaMessage("There was no winner");
                    gameEnd();
                    return true;
                }
                return true;
            }

            //Update our tickers
            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickGameLastTickerUpdate > 1000)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
                }
            }

            //Do we have enough players to start a game?
            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
            }

            //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.king.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }
            return true;
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {	//Let everyone know          

            //TODO: Move this calculation to breakdown() in ScriptArena?
            //Calculate the jackpot for each player
            foreach (Player p in victors.AllPlayers)
            {	//Spectating? 
                if (p.IsSpectator)
                    continue;

                //Obtain the respective rewards
                int cashReward = _config.king.cashReward;
                int experienceReward = _config.king.experienceReward;
                int pointReward = _config.king.pointReward;

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                //Prize winning team
                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }
            _victoryTeam = null;

            //Set off some fireworks using the .lio file to specify locations based on name (starts with 'firework' in name)
            /*
                foreach (LioInfo.Hide firework in _arena._server._assets.Lios.Hides.Where(h => h.GeneralData.Name.ToLower().Contains("firework")))
                    Helpers.Player_RouteExplosion(_arena.Players, (short)firework.HideData.HideId, firework.GeneralData.OffsetX, firework.GeneralData.OffsetY, 0, 0);
            */
            try
            {
                _arena.gameEnd();
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, "Game end exception :: " + e);
            }
        }

        public void giveCrown(Player p)
        {//Give the player a crown and inform the arena
            var v = _playerCrownStatus[p];
            v.crown = true;
            v.crownDeaths = 0;
            v.crownKills = 0;
            List<Player> crowns = _activeCrowns;
            Helpers.Player_Crowns(_arena, true, crowns);
            updateCrownTime(p);
        }

        public void updateCrownTime(Player p)
        {   //Reset the player's counter
            _playerCrownStatus[p].expireTime = Environment.TickCount + (_config.king.expireTime * 1000);
        }

        #region Events

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            //Send them the crowns..
            if (!_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player] = new PlayerCrownStatus(false);
                Helpers.Player_Crowns(_arena, true, _activeCrowns, player);
            }
        }

        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            //Send them the crowns and add them back to list incase a game is in progress
            if (!_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player] = new PlayerCrownStatus(false);
                Helpers.Player_Crowns(_arena, true, _activeCrowns, player);
            }
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            if (_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player].crown = false;
                Helpers.Player_Crowns(_arena, false, _noCrowns);
            }

        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _playerCrownStatus.Clear();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", 1);

            _crownTeams = new List<Team>();
            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            List<Player> crownPlayers = (_config.king.giveSpecsCrowns ? _arena.Players : _arena.PlayersIngame).ToList();

            //Check for first game to spawn flags, otherwise leave them alone
            if (_firstGame)
            {
                _arena.flagSpawn();
                _firstGame = false;
            }

            foreach (var p in crownPlayers)
            {
                _playerCrownStatus[p] = new PlayerCrownStatus();
                giveCrown(p);
            }
            //Everybody is king!
            Helpers.Player_Crowns(_arena, true, crownPlayers);

            return true;
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        public void updateTickers()
        {
            //string format;
            if (_arena.ActiveTeams.Count() > 1)
            {//Show players their crown timer using a ticker
                _arena.setTicker(1, 0, 0, delegate(Player p)
                {
                    if (_playerCrownStatus.ContainsKey(p) && _playerCrownStatus[p].crown)
                        return String.Format("Crown Timer: {0}", (_playerCrownStatus[p].expireTime - Environment.TickCount) / 1000);

                    else
                        return "";
                });
            }
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished, perhaps start a new one

            _arena.sendArenaMessage("Game Over");

            _tickGameStart = 0;
            _tickGameStarting = 0;
            _victoryTeam = null;
            _crownTeams = null;
            //It would be preferable to send false, {emtpy list} here
            //Needs testing
            Helpers.Player_Crowns(_arena, false, _arena.Players.ToList());
            _playerCrownStatus.Clear();

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool playerBreakdown(Player from, bool bCurrent)
        {	//Show some statistics!
            return false;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Game.Breakdown")]
        public bool breakdown()
        {	//Allows additional "custom" breakdown information

            //Always return true;
            return false;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;

            _firstGame = true;
            _victoryTeam = null;
            return true;
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("crown"))
            {   //Give them their crown time
                if (_playerCrownStatus.ContainsKey(player))
                    player.sendMessage(0, "&Crown kills: " + _playerCrownStatus[player].crownKills);
            }

            /* if (command.ToLower().Equals("Skear"))
             {
                 foreach (LioInfo.Hide firework in _arena._server._assets.Lios.Hides.Where(h => h.GeneralData.Name.ToLower().Contains("firework")))
                     Helpers.Player_RouteExplosion(_arena.Players, (short)firework.HideData.HideId, firework.GeneralData.OffsetX, firework.GeneralData.OffsetY, 0, 0);
             }*/
            return true;
        }

        /// <summary>
        /// Handles the spawn of a player
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool bDeath)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {

            return true;
        }

        /// <summary>
        /// Triggered when a player wants to spec and leave the game
        /// </summary>
        [Scripts.Event("Player.LeaveGame")]
        public bool playerLeaveGame(Player player)
        {

            if (_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player].crown = false;
                Helpers.Player_Crowns(_arena, false, _noCrowns);
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        [Scripts.Event("Player.Death")]
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            //Check if a game is running
            if (_activeCrowns.Count == 0)
                return true;

            if (_playerCrownStatus[victim].crown)
            {   //Incr crownDeaths
                _playerCrownStatus[victim].crownDeaths++;

                if (_playerCrownStatus[victim].crownDeaths >= _config.king.deathCount)
                {
                    //Take it away now
                    _playerCrownStatus[victim].crown = false;
                    _noCrowns.Remove(victim);
                    Helpers.Player_Crowns(_arena, false, _noCrowns);
                }

                if (!_playerCrownStatus[killer].crown)
                    _playerCrownStatus[killer].crownKills++;
            }


            //Reset their timer
            if (_playerCrownStatus[killer].crown)
                updateCrownTime(killer);
            else if (_config.king.crownRecoverKills != 0)
            {   //Should they get a crown?
                if (_playerCrownStatus[killer].crownKills >= _config.king.crownRecoverKills)
                {
                    _playerCrownStatus[killer].crown = true;
                    giveCrown(killer);
                }
            }
            return true;
        }
        #endregion
    }
}