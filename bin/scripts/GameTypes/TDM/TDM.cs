using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;


using Assets;

namespace InfServer.Script.GameType_TDM
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_TDM : Scripts.IScript
    {	 ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;				//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private Team _victoryTeam;				//The team currently winning!
        private int _tickGameLastTickerUpdate;
        private int _lastGameCheck;			//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;			//The tick at which the game started (0 == stopped)


        //Settings
        private int _minPlayers;				//The minimum amount of players

        Dictionary<Team, TeamStats> _teamStats;
        
        public class TeamStats
        {
            public int kills { get; set; }
            public int deaths { get; set; }
        }

        Dictionary<String, PlayerStats> _savedPlayerStats;

        public class PlayerStats
        {
            public int kills { get; set; }
            public int deaths { get; set; }
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

	     _savedPlayerStats = new Dictionary<String, PlayerStats>();

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
                _arena.setTicker(1, 3, 0, "Not Enough Players");
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
                _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }
	     //Testing a force game restart for bugged arenas
/*
	     else if (_bGameRunning != true && playing >= _minPlayers)
	     {
		  _bGameRunning == true;
		  _tickGameStart = Environment.TickCount;
		  _tickGameStarting = now;
		  _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
			delegate()
			{
			    //Trigger game start
			    _arena.gameStart();
			}
		  );
	     }
*/
            return true;
        }

        #region Events


        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            // string alias = @"player*";
            if (!_savedPlayerStats.ContainsKey(player._alias.ToString()))
            {
                PlayerStats temp = new PlayerStats();
                temp.deaths = 0;
                temp.kills = 0;
                //if (!_savedPlayerStats.ContainsKey(player._alias.ToString()))
                    _savedPlayerStats.Add(player._alias.ToString(), temp);
            }
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
        {
            
              //We've started!
              _tickGameStart = Environment.TickCount;
		_tickGameStarting = 0;

		//Are we recording stats?
		_arena._saveStats = true;

              _teamStats = new Dictionary<Team, TeamStats>();

              _teamStats[_arena.ActiveTeams.ElementAt(0)] = new TeamStats();
		_teamStats[_arena.ActiveTeams.ElementAt(1)] = new TeamStats();

		//Start a new session for players, clears the old one
              _savedPlayerStats = new Dictionary<String, PlayerStats>();

              foreach (Player p in _arena.Players)
              {
                  //_savedPlayerStats[p._alias.ToString()] = new PlayerStats();
		    PlayerStats temp = new PlayerStats();
		    temp.kills = 0;
		    temp.deaths = 0;
		    _savedPlayerStats.Add(p._alias.ToString(), temp);
              }

              //Let everyone know
              _arena.sendArenaMessage("Game has started!", 1);
		_arena.setTicker(1, 3, _config.deathMatch.timer * 100, "Time Left: ",
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
		    //Team scores
                  format = String.Format("{0}={1} - {2}={3}",
                      _arena.ActiveTeams.ElementAt(0)._name,
                      _teamStats[_arena.ActiveTeams.ElementAt(0)].kills,
                      _arena.ActiveTeams.ElementAt(1)._name,
                      _teamStats[_arena.ActiveTeams.ElementAt(1)].kills);
                  _arena.setTicker(1, 2, 0, format);

		    //Personal scores
		    _arena.setTicker(2, 1, 0, delegate(Player p)
		    {
			 //Update their ticker
			 if (_savedPlayerStats.ContainsKey(p._alias) )
				return "Personal Score: Kills=" + _savedPlayerStats[p._alias.ToString()].kills + " - Deaths=" + _savedPlayerStats[p._alias.ToString()].deaths;

			 return "";
		    } );

		    //1st and 2nd place with mvp (for flags later)
		    IEnumerable<Player> ranking = _arena.PlayersIngame.OrderByDescending(player => _savedPlayerStats[player._alias.ToString()].kills);
		    int idx = 3; format = "";
		    foreach (Player rankers in ranking)
		    {
			 if (!_arena.Players.Contains(rankers))
			     continue;

			 if (idx-- == 0)
			     break;

			 switch (idx)
			 {
			     case 2:
				  format = String.Format("1st: {0}(K={1} D={2})", rankers._alias, 
					_savedPlayerStats[rankers._alias.ToString()].kills, _savedPlayerStats[rankers._alias.ToString()].deaths);
				  break;
			     case 1:
				  format = (format + String.Format(" 2nd: {0}(K={1} D={2})", rankers._alias, 
					_savedPlayerStats[rankers._alias.ToString()].kills, _savedPlayerStats[rankers._alias.ToString()].deaths));
				  break;
			 }
		    }
		    _arena.setTicker(2, 0, 0, format);
		}
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished, perhaps start a new one

            _arena.sendArenaMessage("Game Over!");


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
                  try
                  {
                      from.sendMessage(0, String.Format(format,
                         _teamStats[t].kills, _teamStats[t].deaths,
                          t._name));
                  }
                  catch (Exception e)
                  {
                      Log.write(TLog.Warning, "4 " + e);
                  }
              }

              from.sendMessage(0, "#Individual Statistics Breakdown");

              IEnumerable<Player> rankedPlayers = _arena.PlayersIngame.OrderByDescending(player => _savedPlayerStats[player._alias.ToString()].kills);
        //      IEnumerable<Player> rankedPlayers = _savedPlayerStats.Keys.OrderByDescending(player => _savedPlayerStats[player._alias.ToString()].kills);
              idx = 3;	//Only display top three players

              foreach (Player p in rankedPlayers)
              {
                  if (!_arena.Players.Contains(p))
                      continue;

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
                  try
                  {
                      from.sendMessage(0, String.Format(format,
                          (bCurrent ? _savedPlayerStats[p._alias.ToString()].kills : _savedPlayerStats[p._alias.ToString()].kills),
                          (bCurrent ? _savedPlayerStats[p._alias.ToString()].deaths : _savedPlayerStats[p._alias.ToString()].deaths),
                          p._alias));
                  }
                  catch (Exception e)
                  {
                      Log.write(TLog.Warning, "3 " + e);
                  }
              }
              from.sendMessage(0, "Most Deaths");
              IEnumerable<Player> specialPlayers = _arena.PlayersIngame.OrderByDescending(player => _savedPlayerStats[player._alias.ToString()].deaths);
            //  IEnumerable<Player> specialPlayers = _savedPlayerStats.Keys.OrderByDescending(player => _savedPlayerStats[player].deaths);
              idx = 1; //Only display the top person
              foreach (Player p in specialPlayers)
              {
                  if (!_arena.PlayersIngame.Contains(p))
                      continue;

                  if (idx-- == 0)
                      break;

                  string format = "(D={0}): {1}";
                  try
                  {
                      from.sendMessage(0, String.Format(format,
                          (bCurrent ? _savedPlayerStats[p._alias.ToString()].deaths : _savedPlayerStats[p._alias.ToString()].deaths),
                          p._alias));
                  }
                  catch (Exception e)
                  {
                      Log.write(TLog.Warning, "1 " +e);
                  }
              }
              try
              {
//                  if (!from.IsSpectator)
//                  {
                      string personalFormat = "@Personal Score: (K={0} D={1})";
                      from.sendMessage(0, String.Format(personalFormat,
                          (bCurrent ? _savedPlayerStats[from._alias.ToString()].kills : _savedPlayerStats[from._alias.ToString()].kills),
                          (bCurrent ? _savedPlayerStats[from._alias.ToString()].deaths : _savedPlayerStats[from._alias.ToString()].deaths)));
//                  }
              }
              catch(Exception e)
              {
                  Log.write(TLog.Warning, "2 " + e);
              }
              return false;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {    //Game reset, perhaps start a new one
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
	 /// Handles a player's switch request
	 /// </summary>
	 [Scripts.Event("Player.Switch")]
	 public bool playerSwitch(Player player, LioInfo.Switch swi)
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
            if (_tickGameStart > 0)
            {
                _teamStats[killer._team].kills++;
                _teamStats[victim._team].deaths++;
                try
                {
                    _savedPlayerStats[killer._alias.ToString()].kills++;
                    _savedPlayerStats[victim._alias.ToString()].deaths++;
                }
                catch (Exception e)
                {
                    Log.write(TLog.Warning, "{0},{1}" + e,killer,killer._alias.ToString());
                }
            }
            return true;
        }
        #endregion
    }
}