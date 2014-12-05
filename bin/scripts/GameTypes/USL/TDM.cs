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
using InfServer.Data;

using Assets;

namespace InfServer.Script.GameType_USL
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_USL : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config
        private string FileName;                //This is set by league matches to parse the stats

        private int _tickGameLastTickerUpdate;  //The tick at which the scoreboard was updated
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        private Team team1;                     //Used for normal gameplay and matches
        private Team team2;                     //Used for normal gameplay and matches
        private Team lastTeam1;                 //Records the previous team before overtime starts
        private Team lastTeam2;                 //Records the previous team before overtime starts

        //Game Settings
        private int _minPlayers;				//The minimum amount of players
        private bool _overtime;                 //When the game goes into overtime, the stats still continue
        private int overtimeType = 0;           //Is this single, double or triple overtime?
        private int LeagueSeason = 1;           //Which league season we are in?

        /// <summary>
        /// Current game player stats
        /// </summary>
        private Dictionary<string, PlayerStat> _savedPlayerStats;
        /// <summary>
        /// Only used when overtime is initialized, player stats migrate here
        /// </summary>
        private Dictionary<string, PlayerStat> _lastSavedStats;
        private Team victoryTeam = null;
        DateTime startTime;
        private bool awardMVP = false;                      //Can we award mvp at the end of game?

        /// <summary>
        /// Stores our player information
        /// </summary>
        public class PlayerStat
        {
            public Player player { get; set; }
            public string teamname { get; set; }
            public string alias { get; set; }
            public string squad { get; set; }
            public long points { get; set; }
            public int kills { get; set; }
            public int deaths { get; set; }
            public int assistPoints { get; set; }
            public int playSeconds { get; set; }
            public bool hasPlayed { get; set; }
        }

        /// <summary>
        /// Public event class
        /// </summary>
        public enum Events
        {
            RedBlue,
            GreenYellow
        }

        public int EventType;
        public bool Event = false;                      //Are we doing any special side events?

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
            _savedPlayerStats = new Dictionary<string, PlayerStat>();

            _overtime = false;
            overtimeType = 0;

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

            //If game is running and we dont have enough players
            if (_arena._bGameRunning && playing < _minPlayers)
                //Stop the game
                _arena.gameEnd();

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

                //If this isnt overtime, lets start a new game
                //otherwise we'll wait till the ref starts the match using *startgame
                if (!_overtime)
                    _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
                        delegate()
                        {	//Trigger the game start
                            _arena.gameStart();
                        }
                    );
            }
            return true;
        }

        #region Game Events
        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            team1 = _arena.ActiveTeams.ElementAt(0) != null ? _arena.ActiveTeams.ElementAt(0) : _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.ActiveTeams.Count() > 1 ? _arena.ActiveTeams.ElementAt(1) : _arena.getTeamByName(_config.teams[1].name);

            bool isMatch = _arena._isMatch;

            //If we were doing an event, reset it
            if (Event && isMatch)
                Event = false;

            _savedPlayerStats.Clear();
            foreach (Player p in _arena.Players)
            {
                PlayerStat temp = new PlayerStat();
                temp.teamname = p._team._name;
                temp.alias = p._alias;
                temp.points = 0;
                temp.assistPoints = 0;
                temp.playSeconds = 0;
                temp.squad = p._squad;
                temp.kills = 0;
                temp.deaths = 0;
                temp.player = p;
                temp.hasPlayed = p.IsSpectator ? false : true;

                _savedPlayerStats.Add(p._alias, temp);

                if (isMatch && !p.IsSpectator)
                    //Lets make sure in game players arent spammed banners
                    p._bAllowBanner = false;
            }

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", 1);
            _arena.setTicker(1, 3, _config.deathMatch.timer * 100, "Time Left: ",
                delegate()
                {	//Trigger game end.
                    _arena.gameEnd();
                }
            );

            //Record our start time
            if (_arena._isMatch)
                startTime = DateTime.Now.ToLocalTime();

            updateTickers();
            return true;
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        private void updateTickers()
        {
            //Team scores
            IEnumerable<Team> activeTeams = _arena.Teams.Where(entry => entry.ActivePlayerCount > 0);
            Team A = activeTeams.ElementAt(0) != null ? activeTeams.ElementAt(0) : team1;
            Team B = team2;
            if (activeTeams.Count() > 1)
                B = activeTeams.ElementAt(1) != null ? activeTeams.ElementAt(1) : team2;
            string format = String.Format("{0}={1} - {2}={3}", A._name, A._currentGameKills, B._name, B._currentGameKills);
            _arena.setTicker(1, 2, 0, format);

            //Personal Scores
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

            //1st and 2nd place
            List<Player> ranked = new List<Player>();
            foreach (Player p in _arena.Players)
            {
                if (p == null)
                    continue;
                if (_savedPlayerStats.ContainsKey(p._alias))
                    ranked.Add(p);
            }

            IEnumerable<Player> ranking = ranked.OrderByDescending(player => _savedPlayerStats[player._alias].kills);
            int idx = 3; format = "";
            foreach (Player rankers in ranking)
            {
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
            if (!_arena.recycling)
                _arena.setTicker(2, 0, 0, format);
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

            if (_arena._isMatch)
            {
                foreach (Player p in _arena.PlayersIngame.ToList())
                    p.spec();

                //Update stats only once, OT doesnt matter
                if (!_overtime)
                {
                    foreach (KeyValuePair<string, PlayerStat> pair in _savedPlayerStats)
                    {
                        if (pair.Value.hasPlayed)
                        {
                            if (pair.Value.player == null)
                                continue;
                            //Stats are migrated first due to base.gameEnd being called before this callsync
                            pair.Value.playSeconds = pair.Value.player.StatsLastGame.playSeconds;
                        }
                    }
                }

                if (team1._currentGameKills > team2._currentGameKills)
                {
                    victoryTeam = team1;
                    _arena.sendArenaMessage(String.Format("{0} has won with a {1}-{2} victory!",
                        team1._name,
                        team1._currentGameKills,
                        team2._currentGameKills));
                    foreach (Player p in _arena.Players)
                    {
                        if (p._squad.Contains(team1._name))
                            p.ZoneStat1++;
                        if (p._squad.Contains(team2._name))
                            p.ZoneStat2++;
                    }
                }
                else if (team2._currentGameKills > team1._currentGameKills)
                {
                    victoryTeam = team2;
                    _arena.sendArenaMessage(String.Format("{0} has won with a {1}-{2} victory!",
                        team2._name,
                        team2._currentGameKills,
                        team1._currentGameKills));
                    foreach (Player p in _arena.Players)
                    {
                        if (p._squad.Contains(team2._name))
                            p.ZoneStat1++;
                        if (p._squad.Contains(team1._name))
                            p.ZoneStat2++;
                    }
                }
                else
                {
                    if (_overtime)
                    {
                        switch (++overtimeType)
                        {
                            case 1:
                                _arena.sendArenaMessage("Game ended in a draw. Going into Double OVERTIME!!");
                                break;
                            case 2:
                                _arena.sendArenaMessage("Game ended in a draw. Going into TRIPLE OVERTIME!!");
                                break;
                            case 3:
                                _arena.sendArenaMessage("Game ended in a draw... Quadruple Overtime?");
                                break;
                            case 4:
                            default:
                                _arena.sendArenaMessage("Script is tired of counting, refs take over.");
                                break;
                        }
                    }
                    else
                    {
                        _arena.sendArenaMessage("Game ended in a draw. Going into OVERTIME!!");

                        //Lets migrate stats
                        lastTeam1 = new Team(_arena, _arena._server);
                        lastTeam1._name = team1._name;
                        lastTeam1._currentGameKills = team1._currentGameKills;
                        lastTeam1._currentGameDeaths = team1._currentGameDeaths;

                        lastTeam2 = new Team(_arena, _arena._server);
                        lastTeam2._name = team2._name;
                        lastTeam2._currentGameKills = team2._currentGameKills;
                        lastTeam2._currentGameDeaths = team2._currentGameDeaths;
                        _lastSavedStats = new Dictionary<string, PlayerStat>();
                        foreach (KeyValuePair<string, PlayerStat> pair in _savedPlayerStats)
                        {
                            if (pair.Value.hasPlayed)
                                _lastSavedStats.Add(pair.Key, pair.Value);
                        }
                    }

                    _overtime = true;
                    awardMVP = true;

                    _arena.gameReset();
                    return true;
                }

                //Save to a file
                ExportStats();

                _arena._isMatch = false;
                _overtime = false;
                overtimeType = 0;
                awardMVP = true;
                _arena.gameReset();
            }
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
            victoryTeam = null;

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool individualBreakdown(Player from, bool bCurrent)
        {	//Allows additional "custom" breakdown information

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
            List<Player> rankers = new List<Player>();
            foreach (Player p in _arena.Players)
            {
                if (p == null)
                    continue;
                if (_savedPlayerStats.ContainsKey(p._alias))
                    rankers.Add(p);
            }

            idx = 3;        //Only display top three players
            var rankedPlayerGroups = rankers.Select(player => new
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
        #endregion

        #region Player Events
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
            if (_tickGameStart > 0 && _arena._isMatch && _overtime && death)
            {
                player.spec();
                _arena.sendArenaMessage(String.Format("{0} has died out.", player._alias));

                if (team1.ActivePlayerCount < 1 || team2.ActivePlayerCount < 1)
                    _arena.gameEnd();
            }

            return true;
        }

        /// <summary>
        /// Triggered when a player dies to a bot
        /// </summary>
        [Scripts.Event("Player.BotKill")]
        public bool botKill(Player victim, Bot killer)
        {
            if (_tickGameStart > 0)
            {
                if (_savedPlayerStats.ContainsKey(victim._alias))
                    _savedPlayerStats[victim._alias].deaths++;
            }

            return true;
        }

        /// <summary>
        /// Triggered when a bot dies to a player
        /// </summary>
        [Scripts.Event("Bot.Death")]
        public bool botDeath(Bot victim, Player killer, int weaponID)
        {
            if (_tickGameStart > 0)
            {
                if (_savedPlayerStats.ContainsKey(killer._alias)
                    && killer != null && killer._team != victim._team)
                    _savedPlayerStats[killer._alias].kills++;
            }
            return true;
        }

        /// <summary>
        /// Called when the player successfully joins the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            //Add them to the list if not in it
            if (!_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStat temp = new PlayerStat();
                temp.squad = player._squad;
                temp.assistPoints = 0;
                temp.points = 0;
                temp.playSeconds = 0;
                temp.alias = player._alias;
                temp.deaths = 0;
                temp.kills = 0;
                temp.player = player;
                _savedPlayerStats.Add(player._alias, temp);
            }
            _savedPlayerStats[player._alias].teamname = player._team._name;
            _savedPlayerStats[player._alias].hasPlayed = player.IsSpectator ? false : true;

            if (_arena._isMatch && !player.IsSpectator)
                //Lets make sure to turn banner spamming off
                player._bAllowBanner = false;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            //Add them to the list if not in it
            if (!_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStat temp = new PlayerStat();
                temp.alias = player._alias;
                temp.squad = player._squad;
                temp.assistPoints = 0;
                temp.points = 0;
                temp.playSeconds = 0;
                temp.deaths = 0;
                temp.kills = 0;
                temp.player = player;
                _savedPlayerStats.Add(player._alias, temp);
            }
            _savedPlayerStats[player._alias].hasPlayed = true;

            if (_arena._isMatch && !player.IsSpectator)
                //Lets make sure to turn banner spamming off
                player._bAllowBanner = false;

            //Are we doing an event?
            if (Event)
            {
                //Which event are we doing?
                switch ((Events)EventType)
                {
                    case Events.GreenYellow:
                        //Lets get team stuff
                        Team green = _arena.getTeamByName("Green");
                        Team yellow = _arena.getTeamByName("Yellow");

                        //First do sanity checks
                        if (green == null || yellow == null)
                            break;

                        //Are they the first on the teams?
                        if (green.ActivePlayerCount == 0 || yellow.ActivePlayerCount == 0 || green.ActivePlayerCount == yellow.ActivePlayerCount)
                        {
                            //Great, use it
                            if (green.ActivePlayerCount == yellow.ActivePlayerCount)
                                player.unspec(green);
                            else
                                player.unspec(green.ActivePlayerCount == 0 ? green : yellow);
                            player._lastMovement = Environment.TickCount;
                            //We are returning false so server wont repick us
                            return false;
                        }
                        //Nope, lets do some math
                        player.unspec(green.ActivePlayerCount > yellow.ActivePlayerCount ? yellow : green);
                        player._lastMovement = Environment.TickCount;
                        //Returning false so server wont repick us
                        return false;

                    case Events.RedBlue:
                        //Lets get team stuff
                        Team red = _arena.getTeamByName("Red");
                        Team blue = _arena.getTeamByName("Blue");

                        //First do sanity checks
                        if (red == null || blue == null)
                            break;

                        //Are they the first on the teams?
                        if (red.ActivePlayerCount == 0 || blue.ActivePlayerCount == 0 || red.ActivePlayerCount == blue.ActivePlayerCount)
                        {
                            //Great, use it
                            if (red.ActivePlayerCount == blue.ActivePlayerCount)
                                player.unspec(red);
                            else
                                player.unspec(red.ActivePlayerCount == 0 ? red : blue);
                            player._lastMovement = Environment.TickCount;
                            //We are returning false so server wont repick us
                            return false;
                        }
                        //Nope, lets do some math
                        player.unspec(red.ActivePlayerCount > blue.ActivePlayerCount ? blue : red);
                        player._lastMovement = Environment.TickCount;
                        //Returning false so server wont repick us
                        return false;
                }
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
                PlayerStat temp = new PlayerStat();
                temp.alias = player._alias;
                temp.assistPoints = 0;
                temp.points = 0;
                temp.playSeconds = 0;
                temp.squad = player._squad;
                temp.deaths = 0;
                temp.kills = 0;
                temp.player = player;
                temp.hasPlayed = false;
                _savedPlayerStats.Add(player._alias, temp);
            }
        }

        /// <summary>
        /// Called when the player successfully leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            if (_arena._isMatch && !_overtime)
            {
                if (_savedPlayerStats.ContainsKey(player._alias) && _savedPlayerStats[player._alias].hasPlayed)
                {
                    _savedPlayerStats[player._alias].playSeconds = _tickGameStart > 0 ? player.StatsCurrentGame.playSeconds : player.StatsLastGame != null ? player.StatsLastGame.playSeconds : 0;
                    _savedPlayerStats[player._alias].points = _tickGameStart > 0 ? player.StatsCurrentGame.Points : player.StatsLastGame != null ? player.StatsLastGame.Points : 0;
                    _savedPlayerStats[player._alias].assistPoints = _tickGameStart > 0 ? player.StatsCurrentGame.assistPoints : player.StatsLastGame != null ? player.StatsLastGame.assistPoints : 0;
                }
            }
        }

        /// <summary>
        /// Called when a player leaves the arena
        /// </summary>
        [Scripts.Event("Player.LeaveArena")]
        public void playerLeaveArena(Player player)
        {
            if (_arena._isMatch && !_overtime)
            {
                if (_savedPlayerStats.ContainsKey(player._alias) && _savedPlayerStats[player._alias].hasPlayed)
                {
                    _savedPlayerStats[player._alias].playSeconds = _tickGameStart > 0 ? player.StatsCurrentGame.playSeconds : player.StatsLastGame != null ? player.StatsLastGame.playSeconds : 0;
                    _savedPlayerStats[player._alias].points = _tickGameStart > 0 ? player.StatsCurrentGame.Points : player.StatsLastGame != null ? player.StatsLastGame.Points : 0;
                    _savedPlayerStats[player._alias].assistPoints = _tickGameStart > 0 ? player.StatsCurrentGame.assistPoints : player.StatsLastGame != null ? player.StatsLastGame.assistPoints : 0;
                }
            }
        }

        /// <summary>
        /// Called when a player sends a mod command
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModCommand(Player player, Player recipient, string command, string payload)
        {
            command = (command.ToLower());
            if (command.Equals("mvp") && player.PermissionLevelLocal >= Data.PlayerPermission.Mod)
            {
                if (String.IsNullOrWhiteSpace(payload))
                {
                    player.sendMessage(-1, "Syntax: *mvp alias OR ::*mvp");
                    return false;
                }

                if (!awardMVP)
                {
                    player.sendMessage(-1, "Cannot award yet till the end of a match.");
                    return false;
                }

                Player target = recipient != null ? recipient : _arena.getPlayerByName(payload);
                _arena.sendArenaMessage("MVP award goes to......... ");
                _arena.sendArenaMessage(target != null ? target._alias : payload);

                if (target != null)
                {
                    target.ZoneStat3 += 1;
                    _arena._server._db.updatePlayer(target);
                }

                if (!String.IsNullOrEmpty(FileName))
                {
                    StreamWriter fs = Logic_File.OpenStatFile(FileName, String.Format("Season {0}", LeagueSeason.ToString()));
                    fs.WriteLine();
                    fs.WriteLine("MVP: {0}", target != null ? target._alias : payload);
                    fs.Close();
                }

                awardMVP = false;

                return true;
            }

            if (command.Equals("setscore"))
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: *setscore 1,2  (In order by teamname per scoreboard)");
                    return false;
                }

                if (!payload.Contains(','))
                {
                    player.sendMessage(-1, "Error in syntax, missing comma seperation.");
                    return false;
                }

                string[] args = payload.Split(',');
                if (!Helpers.IsNumeric(args[0]) || !Helpers.IsNumeric(args[1]))
                {
                    player.sendMessage(-1, "Value is not numeric.");
                    return false;
                }

                Int32.TryParse(args[0].Trim(), out team1._currentGameKills);
                Int32.TryParse(args[1].Trim(), out team2._currentGameKills);

                //Immediately notify the change
                updateTickers();

                return true;
            }

            if (command.Equals("poweradd"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                {
                    player.sendMessage(-1, "Nice try.");
                    return false;
                }

                int level = (int)Data.PlayerPermission.ArenaMod;
                //Pm'd?
                if (recipient != null)
                {
                    //Check for a possible level
                    if (!String.IsNullOrWhiteSpace(payload))
                    {
                        try
                        {
                            level = Convert.ToInt16(payload);
                        }
                        catch
                        {
                            player.sendMessage(-1, "Invalid level. Level must be either 1 or 2.");
                            return false;
                        }

                        if (level < 1 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, ":alias:*poweradd level(optional), :alias:*poweradd level (Defaults to 1)");
                            player.sendMessage(0, "Note: there can only be 1 admin level.");
                            return false;
                        }

                        switch (level)
                        {
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient._developer = true;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = true;
                        recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = recipient._alias;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
                else
                {
                    //We arent
                    //Get name and possible level
                    Int16 number;
                    if (String.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "Syntax: *poweradd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                        player.sendMessage(0, "Note: there can only be 1 admin.");
                        return false;
                    }
                    if (payload.Contains(':'))
                    {
                        string[] param = payload.Split(':');
                        try
                        {
                            number = Convert.ToInt16(param[1]);
                            if (number >= 0)
                                level = number;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid level. Possible powering levels are 1 or 2.");
                            return false;
                        }
                        if (level < 1 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, String.Format("Syntax: *poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            player.sendMessage(0, "Note: there can be only 1 admin level.");
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", payload, level));
                    if ((recipient = player._server.getPlayer(payload)) != null)
                    { //They are playing, lets update them
                        switch (level)
                        {
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient._developer = true;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                    }

                    //Lets send it off
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = payload;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
            }

            if (command.Equals("powerremove"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                {
                    player.sendMessage(-1, "Nice try.");
                    return false;
                }

                int level = (int)Data.PlayerPermission.Normal;
                //Pm'd?
                if (recipient != null)
                {
                    //Check for a possible level
                    if (!String.IsNullOrWhiteSpace(payload))
                    {
                        try
                        {
                            level = Convert.ToInt16(payload);
                        }
                        catch
                        {
                            player.sendMessage(-1, "Invalid level. Levels must be between 0 and 2.");
                            return false;
                        }

                        if (level < 0 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, ":alias:*powerremove level(optional), :alias:*powerremove level (Defaults to 0)");
                            return false;
                        }

                        switch (level)
                        {
                            case 0:
                                recipient._permissionStatic = Data.PlayerPermission.Normal;
                                recipient._developer = false;
                                break;
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient.sendMessage(0, String.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = false;
                        recipient._permissionStatic = Data.PlayerPermission.Normal;
                        recipient.sendMessage(0, String.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = recipient._alias;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
                else
                {
                    //We arent
                    //Get name and possible level
                    Int16 number;
                    if (String.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "Syntax: *powerremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                        return false;
                    }
                    if (payload.Contains(':'))
                    {
                        string[] param = payload.Split(':');
                        try
                        {
                            number = Convert.ToInt16(param[1]);
                            if (number >= 0)
                                level = number;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid level. Possible depowering levels are between 0 and 2.");
                            return false;
                        }
                        if (level < 0 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, String.Format("Syntax: *powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", payload, level));
                    if ((recipient = player._server.getPlayer(payload)) != null)
                    { //They are playing, lets update them
                        switch (level)
                        {
                            case 0:
                                recipient._permissionStatic = Data.PlayerPermission.Normal;
                                recipient._developer = false;
                                break;
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient.sendMessage(0, String.Format("You have been depowered to level {0}.", level));
                    }

                    //Lets send it off
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = payload;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
            }

            if (command.Equals("event"))
            {
                var names = Enum.GetNames(typeof(Events));
                if (String.IsNullOrEmpty(payload))
                {
                    //If an event is active, show what it is
                    if (Event)
                        player.sendMessage(0, String.Format("Current active event - {0}", Enum.GetName(typeof(Events), EventType)));
                    string options = String.Join(", ", names);
                    player.sendMessage(-1, String.Format("Syntax: *event <event name> - Options are {0} (use *event off to stop the event)", options));
                    return false;
                }

                if (payload.Equals("off"))
                {
                    Event = false;
                    return true;
                }

                if (!names.Contains(payload, StringComparer.OrdinalIgnoreCase))
                {
                    player.sendMessage(-1, "That is not a valid option.");
                    string options = String.Join(", ", names);
                    player.sendMessage(0, String.Format("Syntax: *event <event name> - Options are {0} (use *event off to stop the event)", options));
                    return false;
                }

                Events eType;
                foreach (string s in names)
                {
                    if (s.Equals(payload, StringComparison.OrdinalIgnoreCase))
                        if (Enum.TryParse(s, out eType))
                        {
                            EventType = (int)eType;
                            Event = true;
                            _arena.sendArenaMessage(String.Format("Event {0} is now ON!", s));
                            return true;
                        }
                }
            }
            return false;
        }
        #endregion

        #region Private Calls
        private int getRating(Team team1, Team team2)
        {
            int rating = 0;



            return rating;

        }

        /// <summary>
        /// Saves all league stats to a file and website
        /// </summary>
        private void ExportStats()
        {   //Sanity checks
            if (_overtime)
            {
                if (_lastSavedStats.Count < 1)
                    return;

                string OT = "OT";
                if (overtimeType > 0)
                {
                    switch (overtimeType)
                    {
                        case 1:
                            OT = "D-OT";
                            break;
                        case 2:
                            OT = "T-OT";
                            break;
                        case 3:
                            OT = "Q-OT";
                            break;
                        default:
                            OT = "OT++";
                            break;
                    }
                }
                //Make the file with current date and filename
                string name1 = lastTeam1._name.Trim(' ');
                string name2 = lastTeam2._name.Trim(' ');
                string filename = String.Format("{0}vs{1} {2}", name1, name2, startTime.ToLocalTime().ToString());
                FileName = filename;
                StreamWriter fs = Logic_File.CreateStatFile(filename, String.Format("Season {0}", LeagueSeason.ToString()));

                fs.WriteLine();
                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = {4}",
                    lastTeam1._name, lastTeam1._currentGameKills, lastTeam1._currentGameDeaths, lastTeam1._name.Equals(victoryTeam._name) ? "Yes" : "No", OT));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team2._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team1._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3} PlaySeconds = {4}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                fs.WriteLine();
                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = {4}",
                    lastTeam2._name, lastTeam2._currentGameKills, lastTeam2._currentGameDeaths, lastTeam2._name.Equals(victoryTeam._name) ? "Yes" : "No", OT));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team1._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team2._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3} PlaySeconds = {4}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Now set the format as per the export file function in the client
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    fs.WriteLine(String.Format("{0},{1},{2},{3},{4},0,{5},0,{6},0,0,0,0",
                        p.Value.alias,
                        p.Value.squad,
                        p.Value.points,
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.assistPoints,
                        p.Value.playSeconds));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Close it
                fs.Close();

                //Report it
                _arena.sendArenaMessage("Stats have been backed up to a file. Please stay till refs are done recording.", 0);
            }
            else
            {
                //Make the file with current date and filename
                string name1 = team1._name.Trim(' ');
                string name2 = team2._name.Trim(' ');
                string filename = String.Format("{0}vs{1} {2}", name1, name2, startTime.ToLocalTime().ToString());
                FileName = filename;
                StreamWriter fs = Logic_File.CreateStatFile(filename, String.Format("Season {0}", LeagueSeason.ToString()));

                fs.WriteLine();
                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = No",
                    team1._name, team1._currentGameKills, team1._currentGameDeaths, team1 == victoryTeam ? "Yes" : "No"));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team2._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team1._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3} PlaySeconds = {4}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                fs.WriteLine();
                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = No",
                    team2._name, team2._currentGameKills, team2._currentGameDeaths, team2 == victoryTeam ? "Yes" : "No"));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team1._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team2._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3} PlaySeconds = {4}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Now set the format as per the export file function in the client
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    fs.WriteLine(String.Format("{0},{1},{2},{3},{4},0,{5},0,{6},0,0,0,0",
                        p.Value.alias,
                        p.Value.squad,
                        p.Value.points,
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.assistPoints,
                        p.Value.playSeconds));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Close it
                fs.Close();

                //Report it
                _arena.sendArenaMessage("Stats have been backed up to a file. Please stay till refs are done recording.", 0);
            }
        }
        #endregion
    }
}