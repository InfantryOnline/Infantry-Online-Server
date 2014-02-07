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

//403,404
//537,701

namespace InfServer.Script.GameType_AxiCTF
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_AxiCTF : Scripts.IScript
    {
        #region Member variables

        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private int _jackpot;					//The game's jackpot so far
        private bool _firstGame;
        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickLastTickerUpdate;      //The tick at which we update our tickers

        private ZonePoll.ZoneChoice team1;
        private ZonePoll.ZoneChoice team2;

        //Settings
        private int _minPlayers;				//The minimum amount of players

        private bool _gameWon = false;
        private ZonePoll zonePoll;              //For picking what map to use next

        #endregion

        #region Member functions

        /// <summary>
        /// Performs script initialization
        /// </summary>
        public bool init(IEventObject invoker)
        {	//Populate our variables
            _arena = invoker as Arena;
            _config = _arena._server._zoneConfig;

            zonePoll = new ZonePoll();

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

            if (zonePoll.isActive)
            {
                foreach (Vehicle v in _arena.Vehicles)
                {
                    Console.WriteLine(v._type.Id);

                    switch (v._type.Id)
                    {
                        case 600:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {

                                    zonePoll.addVote(p._alias, 0, p._team._name);
                                }
                            }
                            break;

                        case 601:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 1, p._team._name);
                                }
                            }
                            break;
                        case 602:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 2, p._team._name);
                                }
                            }
                            break;
                        case 603:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 3, p._team._name);
                                }
                            }
                            break;
                        case 604:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 4, p._team._name);
                                }
                            }
                            break;
                        case 605:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 5, p._team._name);
                                }
                            }
                            break;
                        case 606:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 6, p._team._name);
                                }
                            }
                            break;
                        case 607:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 7, p._team._name);
                                }
                            }
                            break;
                        case 608:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 8, p._team._name);
                                }
                            }
                            break;
                        case 609:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 9, p._team._name);
                                }
                            }
                            break;
                        case 610:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 10, p._team._name);
                                }
                            }
                            break;
                        case 611:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 11, p._team._name);
                                }
                            }
                            break;
                        case 612:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 12, p._team._name);
                                }
                            }
                            break;
                        case 613:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 13, p._team._name);
                                }
                            }
                            break;
                        case 614:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 14, p._team._name);
                                }
                            }
                            break;
                        case 615:
                            foreach (Player p in _arena.PlayersIngame)
                            {
                                if (Helpers.distanceTo(p._state, v._state) < 60)
                                {
                                    zonePoll.addVote(p._alias, 15, p._team._name);
                                }
                            }
                            break;
                    }
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
        #endregion

        #region Events
        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {	//Does this team now have all the flags?
            Team victoryTeam = flag.team;


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

            //Let everyone know
            if (_config.flag.useJackpot)
                _jackpot = (int)Math.Pow(_arena.PlayerCount, 2);
            _arena.sendArenaMessage(String.Format("Victory={0} Jackpot={1}", victors._name, _jackpot), _config.flag.victoryBong);

            //TODO: Move this calculation to breakdown() in ScriptArena?
            //Calculate the jackpot for each player
            foreach (Player p in _arena.Players)
            {	//Spectating? Psh.
                if (p.IsSpectator)
                    continue;
                //Find the base reward
                int personalJackpot;

                if (p._team == victors)
                    personalJackpot = _jackpot * (_config.flag.winnerJackpotFixedPercent / 1000);
                else
                    personalJackpot = _jackpot * (_config.flag.loserJackpotFixedPercent / 1000);

                //Obtain the respective rewards
                int cashReward = personalJackpot * (_config.flag.cashReward / 1000);
                int experienceReward = personalJackpot * (_config.flag.experienceReward / 1000);
                int pointReward = personalJackpot * (_config.flag.pointReward / 1000);

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }

            //Stop the game
            _arena.gameEnd();

        }
        private void spawnDynamicMap()
        {
            VehInfo team1Home = _arena._server._assets.getVehicleByID(Convert.ToInt32(403));
            Helpers.ObjectState t1HomeState = new Protocol.Helpers.ObjectState();
            t1HomeState.positionX = (short)team1.x1;
            t1HomeState.positionY = (short)team1.y1;
            _arena.newVehicle(
                        team1Home,
                        _arena.Teams.ElementAt(1), null,
                        t1HomeState, null);

            VehInfo team1Nme = _arena._server._assets.getVehicleByID(Convert.ToInt32(404));
            Helpers.ObjectState t1NmeState = new Protocol.Helpers.ObjectState();
            t1NmeState.positionX = (short)team2.x2;
            t1NmeState.positionY = (short)team2.y2;
            _arena.newVehicle(
                        team1Nme,
                        _arena.Teams.ElementAt(1), null,
                        t1NmeState, null);

            VehInfo team2Home = _arena._server._assets.getVehicleByID(Convert.ToInt32(403));
            Helpers.ObjectState t2HomeState = new Protocol.Helpers.ObjectState();
            t2HomeState.positionX = (short)team2.x1;
            t2HomeState.positionY = (short)team2.y1;
            _arena.newVehicle(
                        team2Home,
                        _arena.Teams.ElementAt(2), null,
                        t2HomeState, null);

            VehInfo team2Nme = _arena._server._assets.getVehicleByID(Convert.ToInt32(404));
            Helpers.ObjectState t2NmeState = new Protocol.Helpers.ObjectState();
            t2NmeState.positionX = (short)team1.x2;
            t2NmeState.positionY = (short)team1.y2;
            _arena.newVehicle(
                        team2Nme,
                        _arena.Teams.ElementAt(2), null,
                        t2NmeState, null);

        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("co"))
            {
                player.sendMessage(0, "X: " + player._state.positionX + " Y: " + player._state.positionY);
            }

            if (command.ToLower().Equals("vote"))
            {
                Console.WriteLine(command);

                if (!zonePoll.isActive)
                {
                    return true;
                }


                int vote = Convert.ToInt32(payload);

                if (zonePoll.addVote(player._alias, vote - 1, player._team._name))
                {
                    player.sendMessage(0, "Your vote has been cast for choice " + vote + ".");
                }
                else
                {
                    player.sendMessage(0, "Your vote has not been cast, try again");
                }
            }

            return true;
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

            _firstGame = true;

            zonePoll.isActive = true;
            _arena.sendArenaMessage("Pick a base for your team using ?vote # where # is the number of the base you want");
            _arena.sendArenaMessage("Or you can simply step on the tile that represents the base you want");
            _arena.sendArenaMessage("In the event of a tie a random team will have their base changed");
            //Poll players on next map choice
            _arena.setTicker(1, 1, zonePoll._votingTime, "Voting ends: ",
                    delegate()
                    {

                        //Tally up the results and change the warps
                        zonePoll.getMapCoords(out team1, out team2);

                        _arena.sendArenaMessage("---Bases chosen---");
                        _arena.sendArenaMessage("Titan chose " + (team1.id + 1) + " and Collective chose " + (team2.id + 1));

                        //Let everyone know
                        _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

                        //Reset the zone poll
                        zonePoll.Initiate();
                        zonePoll.isActive = false;

                        spawnDynamicMap();


                        //Spawn our flags!
                        _arena.flagSpawn();
                    }
                );

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
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;
            _victoryTeam = null;
            _victoryNotice = 0;

            //Handle voting on new map

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