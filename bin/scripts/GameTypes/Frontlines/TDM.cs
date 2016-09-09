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

namespace InfServer.Script.GameType_FL_TDM
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_FL_TDM : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;          //The tick at which the victory countdown started
        private int _tickNextVictoryNotice;     //The tick at which we will announce the notice
        private int _victoryNotice;             //The number of victory notices that have been fired

        private int _tickGameLastTickerUpdate;
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        //Settings
        private int _minPlayers;				//The minimum amount of players
        private bool _gameWon = false;          //Is this a possible win?

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
            _arena.playtimeTickerIdx = 3; //Sets the global index for our ticker

            _minPlayers = _config.deathMatch.minimumPlayers;
            foreach (Arena.FlagState fs in _arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
                    _minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += onFlagChange;
            }
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

            if (_arena._bGameRunning && playing < _minPlayers)
            {	//Stop the game!
                _arena.gameEnd();
            }

            //Under min players?
            if (playing < _minPlayers)
            {
                _tickGameStarting = 0;
                _arena.setTicker(1, 3, 0, "Not Enough Players");
            }

            //Do we have enough players to start a game?
            if (!_arena._bGameRunning && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
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

            //Is anybody experiencing a victory?
            if (_tickVictoryStart != 0)
            {	//Have they won yet?
                if (now - _tickVictoryStart > (_config.flag.victoryHoldTime * 10))
                {
                    //Yes! Trigger game victory
                    if (!_gameWon)
                        gameVictory(_victoryTeam);
                    return true;
                }
                else
                {	//Do we have a victory notice to give?
                    if (_tickNextVictoryNotice != 0 && now > _tickNextVictoryNotice)
                    {	//Yes! Let's give it
                        int countdown = (_config.flag.victoryHoldTime / 100) - ((now - _tickVictoryStart) / 1000);
                        _arena.sendArenaMessage(String.Format("Victory for {0} in {1} seconds!",
                            _victoryTeam._name, countdown), _config.flag.victoryWarningBong);

                        //Plan the next notice
                        _tickNextVictoryNotice = _tickVictoryStart;
                        _victoryNotice++;

                        if (_victoryNotice == 1 && countdown >= 30)
                            //Default 2/3 time
                            _tickNextVictoryNotice += (_config.flag.victoryHoldTime / 3) * 10;
                        else if (_victoryNotice == 2 || (_victoryNotice == 1 && countdown >= 20))
                            //10 second marker
                            _tickNextVictoryNotice += (_config.flag.victoryHoldTime * 10) - 10000;
                        else
                            _tickNextVictoryNotice = 0;
                    }
                }
            }

            return true;
        }

        #region Game Events
        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {	//Does this team now have all the flags?
            Team victoryTeam = flag.team;

            foreach (Arena.FlagState fs in _arena._flags.Values)
                if (fs.bActive && fs.team != victoryTeam)
                {
                    victoryTeam = null;
                    break;
                }

            if (victoryTeam != null)
            {	//Yes! Victory for them!
                _arena.setTicker(4, 0, _config.flag.victoryHoldTime, "Victory in ");
                _tickNextVictoryNotice = _tickVictoryStart = Environment.TickCount;
                _victoryTeam = victoryTeam;
            }
            else
            {	//Aborted?
                if (_victoryTeam != null && !_gameWon)
                {
                    _tickVictoryStart = 0;
                    _tickNextVictoryNotice = 0;
                    _victoryTeam = null;
                    _victoryNotice = 0;

                    _arena.sendArenaMessage("Victory has been aborted.", _config.flag.victoryAbortedBong);
                    _arena.setTicker(4, 0, 0, "");
                }
            }
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {
            _gameWon = true;

            //Stop the game
            _arena.gameEnd();
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _victoryNotice = 0;

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
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished, perhaps start a new one
            _arena.sendArenaMessage("Game Over");

            foreach (Player p in _arena.PlayersIngame)
            {
                //No rewards for him!
                if (p.StatsLastGame.kills == 0 && p.StatsLastGame.deaths == 0)
                    continue;

                int cash = 300 + (100 * p.StatsLastGame.kills);
                int experience = 170 + (50 * p.StatsLastGame.kills);
                int points = 50 + (100 * p.StatsLastGame.kills);
                p.Cash += cash;
                p.KillPoints += points;
                p.Experience += experience;

                p.sendMessage(0, String.Format("Personal Award: (Cash={0}) (Experience={1}) (Points={2})", cash, experience, points));
                p.resetVars();
                p.syncState();
            }

            _arena.gameReset();
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
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;
            _victoryNotice = 0;
            _gameWon = false;
            _victoryTeam = null;

            return true;
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        private void updateTickers()
        {
            string format = "";

            //1st and 2nd place
            List<Player> ranked = new List<Player>();
            foreach (Player p in _arena.Players)
            {
                if (p == null || p.StatsCurrentGame == null)
                    continue;
                if (p.StatsCurrentGame.kills == 0 && p.StatsCurrentGame.deaths == 0)
                    continue;
                ranked.Add(p);
            }

            //Order by placed kills and deaths
            IEnumerable<Player> ranking = ranked.OrderBy(player => player.StatsCurrentGame.deaths).OrderByDescending(player => player.StatsCurrentGame.kills);
            int idx = 3;
            foreach (Player p in ranking)
            {
                if (idx-- == 0)
                    break;
                switch (idx)
                {
                    case 2:
                        format = String.Format("!1st: {0}(K={1} D={2})", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
                        break;
                    case 1:
                        format = String.Format("{0} 2nd: {1}(K={2} D={3})", format, p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
                        break;
                }
            }
            if (!_arena.recycling)
                _arena.setTicker(2, 1, 0, format);

            //Show the team scores
            if (_arena.ActiveTeams.Count() > 1)
            {
                format = String.Format("{0}={1} - {2}={3}",
                    _arena.ActiveTeams.ElementAt(0)._name,
                    _arena.ActiveTeams.ElementAt(0)._currentGameKills,
                    _arena.ActiveTeams.ElementAt(1)._name,
                    _arena.ActiveTeams.ElementAt(1)._currentGameKills);
                _arena.setTicker(1, 2, 0, format);
            }
        }
        #endregion
    }
}