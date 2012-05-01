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
        private int _tickGameLastTickerUpdate;	
		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;				//The tick at which the game started (0 == stopped)
		//Settings
		private int _minPlayers;				//The minimum amount of players

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
        private List<Player> _activeCrowns
        {
            get { return _playerCrownStatus.Where(p => p.Value.crown).Select(p => p.Key).ToList(); }
        }


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

			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Do we have enough players ingame?
            int playing = _arena.PlayerCount;

            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                _arena.gameReset();
            }

            //Update our tickers
            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickGameLastTickerUpdate > 5000)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
                }
            }

            if (_tickGameStart > 0)
            {
                foreach (var p in _playerCrownStatus)
                    if (now > p.Value.expireTime)
                        p.Value.crown = false;
                //send update pkt
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

        public void updateCrowns()
        {
            List<Player> crowns = _activeCrowns;
            if (crowns.Count > 0)
                Helpers.Player_Crowns(_arena, true, crowns);
            else //End the game!
                _arena.gameEnd();
        }
        public void giveCrown(Player p)
        {
            var v = _playerCrownStatus[p];
            v.crown = true;
            v.crownDeaths = 0;
            v.crownKills = 0;
            updateCrownTime(p);
        }
        public void updateCrownTime(Player p)
        {   //Update the counter for player??
            _playerCrownStatus[p].expireTime = Environment.TickCount + (_config.king.expireTime * 100);
        }

		#region Events
	

		/// <summary>
		/// Called when a player enters the game
		/// </summary>
		[Scripts.Event("Player.Enter")]
		public void playerEnter(Player player)
		{
            if (_tickGameStart != 0)
            {   //Send them the crowns..
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
                _playerCrownStatus.Remove(player);
		}

		/// <summary>
		/// Called when the game begins
		/// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;


            //Let everyone know
            _arena.sendArenaMessage("Game has started!", 1);
            _arena.setTicker(1, 1, _config.deathMatch.timer * 100, "Time Left: ",
            delegate()
            {	//Trigger game end.
                _arena.gameEnd();
            }
            );

            foreach (Team t in _arena.Teams)
            {
                t._calculatedKills = 0;
                t._calculatedDeaths = 0;
            }

            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            List<Player> crownPlayers = (_config.king.giveSpecsCrowns ? _arena.Players : _arena.PlayersIngame).ToList();
            foreach (var p in crownPlayers)
            {
                _playerCrownStatus[p] = new PlayerCrownStatus();
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
                string format;
                if (_arena.ActiveTeams.Count() > 1)
                {
                    format = String.Format("{0}={1} - {2}={3}",
                        _arena.ActiveTeams.ElementAt(0)._name,
                        _arena.ActiveTeams.ElementAt(0)._calculatedKills,
                        _arena.ActiveTeams.ElementAt(1)._name,
                        _arena.ActiveTeams.ElementAt(1)._calculatedKills);
                    _arena.setTicker(1, 0, 0, format);
                }


        }

		/// <summary>
		/// Called when the game ends
		/// </summary>
		[Scripts.Event("Game.End")]
		public bool gameEnd()
		{	//Game finished, perhaps start a new one

            _arena.sendArenaMessage("Game Over");

            _arena.breakdown(false);

			_tickGameStart = 0;
			_tickGameStarting = 0;
			_victoryTeam = null;

            //It would be preferable to send false, {emtpy list} here
            //Needs testing
            Helpers.Player_Crowns(_arena, false, _arena.Players.ToList());
            _playerCrownStatus.Clear();

			return true;
		}

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Game.Breakdown")]
        public bool breakdown()
        {	//Allows additional "custom" breakdown information


            //Always return true;
            return true;
        }

		/// <summary>
		/// Called to reset the game state
		/// </summary>
		[Scripts.Event("Game.Reset")]
		public bool gameReset()
		{	//Game reset, perhaps start a new one
			_tickGameStart = 0;
			_tickGameStarting = 0;

			_victoryTeam = null;

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
            _playerCrownStatus[player].crown = false;
            updateCrowns();
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
            if (_playerCrownStatus[victim].crown)
            {   //Incr crownDeaths
                _playerCrownStatus[victim].crownDeaths++;

                if (_playerCrownStatus[victim].crownDeaths > _config.king.deathCount)
                {   //Take it away now
                    _playerCrownStatus[victim].crown = false;
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
                    giveCrown(killer);
            }

            updateCrowns();
			return true;
		}
		#endregion
	}
}