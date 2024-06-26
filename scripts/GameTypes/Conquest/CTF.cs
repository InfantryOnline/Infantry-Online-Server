﻿using System;
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

namespace InfServer.Script.GameType_CONQUEST
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_CONQUEST : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private int _jackpot;					//The game's jackpot so far

        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickLastTickerUpdate;       //The tick at which we update our tickers
        //Settings
        private int _minPlayers;				//The minimum amount of players

        private bool _gameWon = false;

        private LowerLevel caverns;

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

            caverns = new LowerLevel();

            _minPlayers = Int32.MaxValue;

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
                    _minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += onFlagChange;
            }

            if (_minPlayers == Int32.MaxValue)
                //No flags? Run blank games
                _minPlayers = 1;

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
            //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.flag.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }

            //Update our tickers
/*            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickLastTickerUpdate > 1500)
                {
                    updateTickers();
                    _tickLastTickerUpdate = now;
                }
            }
 */
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

        #region Events
        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {	//Does this team now have all the flags?
            Team victoryTeam = flag.team;

            try
            {
                caverns.FlagChange(flag.team._id, new Vector2(flag.posX, flag.posY));
            }
            catch (Exception)
            {
            }
            foreach (Arena.FlagState fs in _arena._flags.Values)
                if (fs.bActive && fs.team != victoryTeam)
                    victoryTeam = null;

            if (victoryTeam != null)
            {	//Yes! Victory for them!
                _arena.setTicker(1, 1, _config.flag.victoryHoldTime, "Victory in ");
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
                    _arena.setTicker(1, 1, 0, "");
                }
            }
        }

        public void updateTickers()
        {
            int kills = 0;
            int deaths = 0;
            string format;

            if (_arena.ActiveTeams.Count() > 1)
            {
                //Team scores
                format = String.Format("{0}={1} - {2}={3}",
                    _arena.ActiveTeams.ElementAt(0)._name,
                   _arena.ActiveTeams.ElementAt(0)._currentGameKills,
                    _arena.ActiveTeams.ElementAt(1)._name,
                    _arena.ActiveTeams.ElementAt(1)._currentGameKills);
                _arena.setTicker(1, 2, 0, format);

                //Personal scores
                _arena.setTicker(2, 1, 0, delegate(Player p)
                {
                    if (p.StatsCurrentGame != null)
                    {
                        kills = p.StatsCurrentGame.kills;
                        deaths = p.StatsCurrentGame.deaths;
                    }
                    //Update their ticker
                    return "Personal Score: Kills=" + kills + " - Deaths=" + deaths;

                });

                //1st and 2nd place with mvp (for flags later)
                IEnumerable<Player> ranking = _arena.PlayersIngame.OrderByDescending(player => player.StatsCurrentGame.kills);
                int idx = 3; format = "";
                foreach (Player rankers in ranking)
                {
                    int rKills = 0;
                    int rDeaths = 0;

                    if (rankers.StatsCurrentGame != null)
                    {
                        rKills = rankers.StatsCurrentGame.kills;
                        rDeaths = rankers.StatsCurrentGame.deaths;
                    }

                    if (idx-- == 0)
                        break;

                    switch (idx)
                    {
                        case 2:
                            format = String.Format("1st: {0}(K={1} D={2})", rankers._alias,
                            rKills, rDeaths);
                            break;
                        case 1:
                            format = (format + String.Format(" 2nd: {0}(K={1} D={2})", rankers._alias,
                              rKills, rDeaths));
                            break;
                    }
                }
//                if (!_arena.recycling)
//                    _arena.setTicker(2, 0, 0, format);
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
        {
            //Reset Flags
            _arena.flagReset();
            _arena.flagSpawn();
            
            //We've started!
            caverns = new LowerLevel();
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _victoryNotice = 0;

            //Spawn our flags!
            _arena.flagSpawn();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

            //Signal that a game has not been won yet
            _gameWon = false;
            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished, perhaps start a new one
            _arena.flagReset();
            _tickGameStart = 0;            
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;
            _victoryTeam = null;
            _victoryNotice = 0;

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
            _arena.flagReset();
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;

            _gameWon = false;

            _victoryTeam = null;

            return true;
        }
        
        /// <summary>
        /// Handles a player's portal request
        /// </summary>
        [Scripts.Event("Player.Portal")]
        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            _arena.sendArenaMessage("Portal ID: " + portal.GeneralData.Id);

            Vector2 SpawnLocation = 
                caverns.GetNextWarp(player._team._id, portal.GeneralData.Id);

            if (SpawnLocation != null)
            {
                //Just warp them there and ignore everything else
             //   player.warp(SpawnLocation.x, SpawnLocation.y);
                return false;
            }

            List<Arena.FlagState> carried = _arena._flags.Values.Where(flag => flag.carrier == player).ToList();

            foreach (Arena.FlagState carry in carried)
            {   //If the terrain number is 0-15

                int terrainNum = player._arena.getTerrainID(player._state.positionX, player._state.positionY);
                if (terrainNum >= 0 && terrainNum <= 15)
                {   //Check the FlagDroppableTerrains for that specific terrain id

                    if (carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0)
                        _arena.flagResetPlayer(player);
                }
            }

            return true;
        }

        #endregion
    }
}