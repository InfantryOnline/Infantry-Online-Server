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

namespace InfServer.Script.GameType_USL_TDM
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_TDM : Scripts.IScript
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
        public bool _isMatch;

        Dictionary<Team, TeamStats> _teamStats;

        public class TeamStats
        {
            public int kills { get; set; }
            public int deaths { get; set; }
            public int points { get; set; }
            public int rating { get; set; }
            public bool win { get; set; }
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

            _minPlayers = _config.deathMatch.minimumPlayers;

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
                if (now - _tickGameLastTickerUpdate > 1500)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
                }
            }

            //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.deathMatch.startDelay * 100, "Next game: ",
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

            //Are we recording stats?
            _arena._saveStats = _isMatch;

            _teamStats = new Dictionary<Team, TeamStats>();

            _teamStats[_arena.ActiveTeams.ElementAt(0)] = new TeamStats();
            _teamStats[_arena.ActiveTeams.ElementAt(1)] = new TeamStats();


            //Let everyone know
            _arena.sendArenaMessage("Game has started!", 1);
            _arena.setTicker(1, 1, _config.deathMatch.timer * 100, "Time Left: ",
            delegate()
            {	//Trigger game end.
                _arena.gameEnd();
            }
            );

           
            return true;
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        public void updateTickers()
        {

            //Make sure stats are up-to-date
            foreach (Team t in _arena.ActiveTeams)
                t.precalculateStats(true);


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


			_tickGameStart = 0;
			_tickGameStarting = 0;
			_victoryTeam = null;

			return true;
		}

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool breakdown(Player from, bool bCurrent)
        {	//Allows additional "custom" breakdown information

				from.sendMessage(0, "#Team Statistics Breakdown");

				//Make sure stats are up-to-date
				foreach (Team t in _arena.Teams)
					t.precalculateStats(bCurrent);

				IEnumerable<Team> activeTeams = _arena.Teams.Where(entry => entry.ActivePlayerCount > 0);
				IEnumerable<Team> rankedTeams = activeTeams.OrderByDescending(entry => entry._calculatedKills);
				int idx = 3;	//Only display top three teams

                foreach (Team t in rankedTeams)
                {
                    if (idx-- == 0)
                        break;

                    string format = "!3rd (K={0} D={1}): {2}";

                    switch (idx)
                    {
                        case 2:
                            format = "!1st (K={0} D={1}): {2}";
                            break;
                        case 1:
                            format = "!2nd (K={0} D={1}): {2}";
                            break;
                    }

                    from.sendMessage(0, String.Format(format,
                        t._calculatedKills, t._calculatedDeaths,
                        t._name));
                }

                    from.sendMessage(0, "#Individual Statistics Breakdown");

                    IEnumerable<Player> rankedPlayers = _arena.PlayersIngame.OrderByDescending(player => (bCurrent ? player.StatsCurrentGame.kills : player.StatsLastGame.kills));
                
                    idx = 3;	//Only display top three players

                    foreach (Player p in rankedPlayers)
                    {
                        if (idx-- == 0)
                            break;

                        string format = "!3rd (K={0} D={1}): {2}";

                        switch (idx)
                        {
                            case 2:
                                format = "!1st (K={0} D={1}): {2}";
                                break;
                            case 1:
                                format = "!2nd (K={0} D={1}): {2}";
                                break;
                        }

                        from.sendMessage(0, String.Format(format,
                            (bCurrent ? p.StatsCurrentGame.kills : p.StatsLastGame.kills),
                            (bCurrent ? p.StatsCurrentGame.deaths : p.StatsLastGame.deaths),
                            p._alias));
                    }
                    string personalFormat = "@Personal Score: (K={0} D={1})";
                    from.sendMessage(0, String.Format(personalFormat,
                        (bCurrent ? from.StatsCurrentGame.kills : from.StatsLastGame.kills),
                        (bCurrent ? from.StatsCurrentGame.deaths : from.StatsLastGame.deaths)));


                    if (_arena._isMatch)
                    {
                        //Who won?
                        _teamStats[rankedTeams.First()].win = true;
                        _teamStats[rankedTeams.ElementAt(1)].win = false;

                        foreach (Team team in _teamStats.Keys)
                        {

                            int totalPoints = 0;

                            foreach (Player player in team.ActivePlayers)
                            {
                                
                                //Win?
                                if (_teamStats[team].win)
                                    player.ZoneStat1++;
                                else
                                    player.ZoneStat2++;


                                //Count out the total points.
                                totalPoints += (int)player.StatsCurrentGame.Points;


                                //Move on...
                                player.migrateStats();
                                _arena._server._db.updatePlayer(player);
                            }
                            _teamStats[team].points = totalPoints;
                        }

                        CS_SquadMatch<InfServer.Data.Database>.SquadStats wStats = new CS_SquadMatch<InfServer.Data.Database>.SquadStats();
                        wStats.kills = _teamStats[rankedTeams.ElementAt(0)].kills;
                        wStats.deaths = _teamStats[rankedTeams.ElementAt(0)].deaths;
                        wStats.points = _teamStats[rankedTeams.ElementAt(0)].points;

                        CS_SquadMatch<InfServer.Data.Database>.SquadStats lStats = new CS_SquadMatch<InfServer.Data.Database>.SquadStats();
                        lStats.kills = _teamStats[rankedTeams.ElementAt(1)].kills;
                        lStats.deaths = _teamStats[rankedTeams.ElementAt(1)].deaths;
                        lStats.points = _teamStats[rankedTeams.ElementAt(1)].points;

                        //Finally! report the match
                        _arena._server._db.reportMatch(rankedTeams.ElementAt(0).ActivePlayers.First()._squadID,
                            rankedTeams.ElementAt(1).ActivePlayers.First()._squadID,
                            wStats, lStats);

                        _arena.sendArenaMessage("Stats have been reported.");
                    }
              
			
            return false;
        }

        public int getRating(Team team1, Team team2)
        {
            int rating = 0;



            return rating;
            
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
            if (_isMatch)
            {
                _teamStats[killer._team].kills++;
                _teamStats[victim._team].deaths++;
            }
			return true;
		}
		#endregion
	}
}