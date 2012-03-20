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

namespace InfServer.Script.GameType_Dodgeball
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_Dodgeball : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config

		private Team _victoryTeam;				//The team currently winning!
        private Dictionary<Team, int> _teams;   //Our teams, and how many players left.
        private Random _rand;		
        Team team1;
        Team team2;
        int team1Count;
        int team2Count;
        Dictionary<Player, bool> inPlayers;
		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;				//The tick at which the game started (0 == stopped)
		//Settings
		private int _minPlayers;				//The minimum amount of players
        private bool bVictory = false;



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
            _rand = new Random();

            team1 = _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.getTeamByName(_config.teams[1].name);
            _minPlayers = 2;
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

            if (_tickGameStart != 0)
            {
                if (team1Count == 0 || team2Count == 0)
                {   //Victory
                    _arena.gameEnd();
                }
            }

           //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, 20 * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start

                        _arena.gameStart();
                    }
                );
            }
            return true;
        }

		#region Events
	

		/// <summary>
		/// Called when a player enters the game
		/// </summary>
		[Scripts.Event("Player.Enter")]
		public void playerEnter(Player player)
		{
		}

		/// <summary>
		/// Called when a player leaves the game
		/// </summary>
		[Scripts.Event("Player.Leave")]
		public void playerLeave(Player player)
		{
		}

		/// <summary>
		/// Called when the game begins
		/// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            //Spread players across the public teams
            IEnumerable<Player> players = _arena.PlayersIngame.OrderBy(plyr => _rand.Next(0, 200));

            List<Team> publicTeams = new List<Team>();

            publicTeams.Add(_arena.getTeamByName("Cougars"));
            publicTeams.Add(_arena.getTeamByName("Titans"));

            int playerCount = _arena.PlayerCount;
            int _teamCount = 2;
            int playerPerTeam = (int)Math.Ceiling((float)playerCount / (float)_teamCount);

            inPlayers = new Dictionary<Player, bool>();

            foreach (Player p in _arena.PlayersIngame)
            {
                inPlayers.Add(p, true);
                int x = 0;
                p.setVar("Hits", x);
            }
            team1Count = team1.ActivePlayerCount;
            team2Count = team2.ActivePlayerCount;

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

            return true;
        }

		/// <summary>
		/// Called when the game ends
		/// </summary>
		[Scripts.Event("Game.End")]
		public bool gameEnd()
		{	//Game finished, perhaps start a new one


			_tickGameStart = 0;
			_tickGameStarting = 0;
			_victoryTeam = null;



            int pcount = 0;

            if (team1Count > 0)
            {
                _victoryTeam = team1;
                pcount = team1Count;
            }
            else
            {
                _victoryTeam = team2;
                pcount = team2Count;
            }

            _arena.sendArenaMessage("Game Over");
            _arena.sendArenaMessage(String.Format("&{0} are victorious with {1} player(s) left", _victoryTeam._name, pcount));


            IEnumerable<Player> rankedPlayers;
            int idx;
            rankedPlayers = _arena.PlayersIngame.OrderByDescending(
                    p => p.getVarInt("Hits"));
            idx = 3;	//Only display top three players

            foreach (Player p in rankedPlayers)
            {

                if (idx-- == 0)
                    break;

                //Set up the format
                string format = "!3rd - (Hits={0}): {1}";

                switch (idx)
                {
                    case 2:
                        format = "!1st - (Hits={0}): {1}";
                        break;
                    case 1:
                        format = "!2nd - (Hits={0}): {1}";
                        break;
                }

                _arena.sendArenaMessage(String.Format(format, p.getVarInt("Hits"), p._alias));
            }

            foreach (Player p in _arena.Players)
                p.resetVars();

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
            if (inPlayers[player])
            {
                if (player._team == team1)
                    team1Count--;
                else
                    team2Count--;

                inPlayers[player] = false;
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
            if (victim._team == team1)
                team1Count--;
            else
                team2Count--;


            killer.setVar("Hits", killer.getVarInt("Hits") + 1);
            inPlayers[victim] = false;
			return true;
		}
		#endregion
	}
}