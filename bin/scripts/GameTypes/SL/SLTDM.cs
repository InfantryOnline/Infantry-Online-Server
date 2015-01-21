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

namespace InfServer.Script.GameType_SLTDM
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_SLTDM : Scripts.IScript
    {	 ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;				//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private int _jackpot;					//The game's jackpot so far

        private Team _victoryTeam;				//The team currently winning!
        private Team team1;                     //Used for normal gameplay and matches
        private Team team2;                     //Used for normal gameplay and matches
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done

        private int _tickGameLastTickerUpdate;
        private int _lastGameCheck;			    //The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;			    //The tick at which the game started (0 == stopped)

        //Settings
        private int _minPlayers;				//The minimum amount of players
        private bool _gameWon = false;

        private Dictionary<String, PlayerStats> _savedPlayerStats;
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
            _arena.playtimeTickerIdx = 3; //Sets the global ticker index

            _minPlayers = _config.deathMatch.minimumPlayers;

            //For flag games
            foreach (Arena.FlagState fs in _arena._flags.Values)
                //Register our flag change events
                fs.TeamChange += onFlagChange;

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

            //If game is running and we don't have enough players
            if (_arena._bGameRunning && playing < _minPlayers)
            {   //Stop the game!
                _arena.gameEnd();
            }

            //If were under min players, show the not enough players
            if (playing < _minPlayers)
            {
                _tickGameStarting = 0;
                _arena.setTicker(1, 3, 0, "Not Enough Players");
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

            //Is anybody experiencing a victory?
            if (_tickVictoryStart > 0)
            {	//Have they won yet?
                if (now - _tickVictoryStart > (_config.flag.victoryHoldTime * 10) && !_gameWon)
                {
                    //Yes! Trigger game victory
                    _gameWon = true; // game won
                    gameVictory(_victoryTeam);
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
                        else if (_victoryNotice == 2 || (_victoryNotice == 1 && countdown < 30))
                        {
                            //10 second marker
                            _tickNextVictoryNotice += (_config.flag.victoryHoldTime * 10) - 10000;
                        }
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
                    victoryTeam = null;

            if (victoryTeam != null)
            {	//Yes! Victory for them!
                _arena.setTicker(1, 0, _config.flag.victoryHoldTime, "Victory in ");
                _tickNextVictoryNotice = _tickVictoryStart = Environment.TickCount;
                _victoryTeam = victoryTeam;
            }
            else
            {	//Aborted?
                if (_victoryTeam != null && !_gameWon)
                {
                    _tickVictoryStart = 0;
                    _tickNextVictoryNotice = 0;

                    _arena.sendArenaMessage("Victory has been aborted.", _config.flag.victoryAbortedBong);
                    _arena.setTicker(1, 0, 0, "");
                    _victoryTeam = null;
                }
            }
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {	//Let everyone know
            if (_arena._bIsPublic)
            {
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
                    int experienceReward = personalJackpot * (_config.flag.experienceReward / 1000);

                    p.sendMessage(0, String.Format("Your Personal Reward: Experience={0}", experienceReward));
                    p.Experience += experienceReward;
                }
            }

            //Stop the game
            _arena.gameEnd();
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
            _gameWon = false;
            _victoryTeam = null;

            team1 = _arena.ActiveTeams.ElementAt(0) != null ? _arena.ActiveTeams.ElementAt(0) : _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.ActiveTeams.Count() > 1 ? _arena.ActiveTeams.ElementAt(1) : _arena.getTeamByName(_config.teams[1].name);

            //Start a new session for players, clears the old one
            _savedPlayerStats.Clear();
            foreach (Player p in _arena.Players)
            {
                PlayerStats temp = new PlayerStats();
                temp.kills = 0;
                temp.deaths = 0;
                _savedPlayerStats.Add(p._alias, temp);
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
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished, perhaps start a new one
            _arena.sendArenaMessage("Game Over!");

            _arena.gameReset();
            return true;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {    //Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;

            _victoryTeam = null;
            _gameWon = false;

            return true;
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        public void updateTickers()
        {
            //Team scores
            string format = String.Format("{0}={1} - {2}={3}", team1._name, team1._currentGameKills, team2._name, team2._currentGameKills);
            _arena.setTicker(1, 2, 0, format);

            //Personal scores
            _arena.setTicker(2, 1, 0, delegate(Player p)
            {
                //Update their ticker
                if (_savedPlayerStats.ContainsKey(p._alias))
                    return String.Format("HP={0}          Personal Score: Kills={1} - Deaths={2}",
                        p._state.health,
                        _savedPlayerStats[p._alias].kills,
                        _savedPlayerStats[p._alias].deaths);

                return "";
            });

            //1st and 2nd place with mvp (for flags later)
            IEnumerable<Player> ranking = _arena.Players.OrderByDescending(player => _savedPlayerStats[player._alias].kills);
            int idx = 3; format = "";
            foreach (Player rankers in ranking)
            {
                if (rankers == null)
                    continue;

                if (idx-- == 0)
                    break;

                switch (idx)
                {
                    case 2:
                        format = String.Format("1st: {0}(K={1} D={2})", rankers._alias,
                          _savedPlayerStats[rankers._alias].kills, _savedPlayerStats[rankers._alias].deaths);
                        break;
                    case 1:
                        format = (format + String.Format(" 2nd: {0}(K={1} D={2})", rankers._alias,
                          _savedPlayerStats[rankers._alias].kills, _savedPlayerStats[rankers._alias].deaths));
                        break;
                }
            }
            if (!_arena.recycling && _victoryTeam == null)
                _arena.setTicker(2, 0, 0, format);
        }
        #endregion

        #region Player Events
        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool breakdown(Player from, bool bCurrent)
        {	//Allows additional "custom" breakdown information

            if (from == null)
                return false;

            from.sendMessage(0, "#Team Statistics Breakdown");
            IEnumerable<Team> activeTeams = _arena.Teams.Where(entry => entry.ActivePlayerCount > 0);
            IEnumerable<Team> rankedTeams = activeTeams.OrderByDescending(entry => entry._currentGameKills);
            int idx = 3;	//Only display top three teams
            foreach (Team t in rankedTeams)
            {
                if (t == null)
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

                from.sendMessage(0, String.Format(format,
                    t._currentGameKills, t._currentGameDeaths,
                    t._name));
            }

            from.sendMessage(0, "#Individual Statistics Breakdown");
            idx = 3;        //Only display top three players
            List<Player> plist = new List<Player>();
            foreach (Player p in _arena.Players)
            {
                if(p == null)
                    continue;
                plist.Add(p);
            }
            var rankedPlayerGroups = plist.Select(player => new 
            { 
                Alias = player._alias, 
                Kills = _savedPlayerStats[player._alias].kills, 
                Deaths = _savedPlayerStats[player._alias].deaths 
            })
            .GroupBy(player => player.Kills)
            .OrderByDescending(k => k.Key)
            .Take(idx)
            .Select(group => group.OrderBy(player => player.Deaths));

            foreach (var group in rankedPlayerGroups)
            {
                if (idx <= 0) 
                    break;

                string placeWord = "";
                string format = " (K={0} D={1}): {2}";
                switch (idx)
                {
                    case 3:
                        placeWord = "!1st";
                        break;
                    case 2:
                        placeWord = "!2nd";
                        break;
                    case 1:
                        placeWord = "!3rd";
                        break;
                }

                idx -= group.Count();
                from.sendMessage(0, String.Format(placeWord + format, group.First().Kills, 
                    group.First().Deaths, 
                    String.Join(", ", group.Select(g => g.Alias))));
            }
            /*
            IEnumerable<Player> rankedPlayers = _arena.Players.OrderByDescending(player => _savedPlayerStats[player._alias].kills);
            idx = 3;	//Only display top three players
            foreach (Player p in rankedPlayers)
            {
                if (p == null)
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

                if (_savedPlayerStats[p._alias] != null)
                {
                    from.sendMessage(0, String.Format(format, _savedPlayerStats[p._alias].kills,
                        _savedPlayerStats[p._alias].deaths,
                        p._alias));
                }
            }
            */

            IEnumerable<Player> specialPlayers = _arena.Players.OrderByDescending(player => _savedPlayerStats[player._alias].deaths);
            int topDeaths = _savedPlayerStats[specialPlayers.ElementAt(0)._alias].deaths, deaths = 0;
            if (topDeaths > 0)
            {
                from.sendMessage(0, "Most Deaths");
                int i = 0;
                List<string> mostDeaths = new List<string>();
                foreach (Player p in specialPlayers)
                {
                    if (p == null)
                        continue;

                    if (_savedPlayerStats[p._alias] != null)
                    {
                        deaths = _savedPlayerStats[p._alias].deaths;
                        if (deaths == topDeaths)
                        {
                            if (i++ >= 1)
                                mostDeaths.Add(p._alias);
                            else
                                mostDeaths.Add(String.Format("(D={0}): {1}", deaths, p._alias));
                        }
                    }
                }
                string s = String.Join(", ", mostDeaths.ToArray());
                from.sendMessage(0, s);
            }

            if (_savedPlayerStats[from._alias] != null)
            {
                string personalFormat = "!Personal Score: (K={0} D={1})";
                from.sendMessage(0, String.Format(personalFormat,
                    _savedPlayerStats[from._alias].kills,
                    _savedPlayerStats[from._alias].deaths));
            }

            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            if (!_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStats temp = new PlayerStats();
                temp.deaths = 0;
                temp.kills = 0;
                _savedPlayerStats.Add(player._alias, temp);
            }
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            if (_tickGameStart > 0)
            {
                if (_savedPlayerStats.ContainsKey(killer._alias))
                    _savedPlayerStats[killer._alias].kills++;
                if (_savedPlayerStats.ContainsKey(victim._alias))
                    _savedPlayerStats[victim._alias].deaths++;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player has spawned
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool death)
        {
            //We only want to trigger end game when the last team member died out
            if (_tickGameStart > 0 && death)
            {
                if (_savedPlayerStats[player._alias] != null && _savedPlayerStats[player._alias].deaths >= 3)
                {
                    player.spec();
                    _arena.sendArenaMessage(String.Format("{0} has died out.", player._alias));

                    if (team1.ActivePlayerCount < 1 || team2.ActivePlayerCount < 1)
                        _arena.gameEnd();
                }
            }
            return true;
        }
        #endregion
    }
}