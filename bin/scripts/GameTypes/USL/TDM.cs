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

namespace InfServer.Script.GameType_USL
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_TDM : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config

        private int _tickGameLastTickerUpdate;	
		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;				//The tick at which the game started (0 == stopped)

		//Settings
		private int _minPlayers;				//The minimum amount of players
        private bool _overtime;                 //When the game goes into overtime, the stats still continue
        private int overtimeType;               //Is this single, double or triple overtime?

        Dictionary<Team, TeamStats> _teamStats;
        Dictionary<String, PlayerStats> _savedPlayerStats;
        private bool awardMVP = false;

        public class TeamStats
        {
            public long squadID { get; set; }
            public int kills { get; set; }
            public int deaths { get; set; }
            public int points { get; set; }
            public int rating { get; set; }
            public bool win { get; set; }
        }

        public class PlayerStats
        {
            public Player player { get; set; }
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
            _savedPlayerStats = new Dictionary<string, PlayerStats>();
            _teamStats = new Dictionary<Team, TeamStats>();
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
                _arena.playtimeTickerIdx = 3;
                _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
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
		/// Called when the game begins
		/// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            //Are we recording stats?
            _arena._saveStats = _arena._isMatch;

            //Starts a new session, clears the old ones
            if (!_overtime)
            {
                _savedPlayerStats.Clear();
                _teamStats.Clear();

                _teamStats[_arena.ActiveTeams.ElementAt(0)] = new TeamStats();
                _teamStats[_arena.ActiveTeams.ElementAt(1)] = new TeamStats();
            }
            Console.WriteLine(_arena.ActiveTeams.ElementAt(0)._name);
            Console.WriteLine(_teamStats.ElementAt(0).Key._name);

            foreach (Player p in _arena.Players)
            {
                PlayerStats temp = new PlayerStats();
                temp.kills = 0;
                temp.deaths = 0;
                temp.player = p;
                //Since overtime wont clear the list, reset scores
                if (!_savedPlayerStats.ContainsKey(p._alias))
                    _savedPlayerStats.Add(p._alias, temp);
                else
                    _savedPlayerStats[p._alias] = temp;

                if (_arena._isMatch && _teamStats.ContainsKey(p._team))
                    if (_teamStats[p._team].squadID < 1)
                        _teamStats[p._team].squadID = p._squadID;
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
        private void updateTickers()
        {
            string format;
            if (_arena.ActiveTeams.Count() > 1)
            {
                //Team Scores
                format = String.Format("{0}={1} - {2}={3}",
                    _arena.ActiveTeams.ElementAt(0)._name,
                    _arena.ActiveTeams.ElementAt(0)._currentGameKills,
                    _arena.ActiveTeams.ElementAt(1)._name,
                    _arena.ActiveTeams.ElementAt(1)._currentGameKills);
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
                    }
                );

                //1st and 2nd place
                IEnumerable<Player> ranking = _arena.Players.OrderByDescending(player => _savedPlayerStats[player._alias].kills);
                int idx = 3; format = "";
                foreach (Player rankers in ranking)
                {
                    if (rankers == null)
                        continue;

                    if (!_arena.Players.Contains(rankers))
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

            if (_arena._isMatch)
            {
                //Who won?
                TeamStats team1 = _teamStats.ElementAt(0).Value;
                TeamStats team2 = _teamStats.ElementAt(1).Value;

                if (team1.kills > team2.kills)
                {
                    team1.win = true;
                    team2.win = false;
                    _arena.sendArenaMessage(String.Format("{0} has won with a {1}-{2} victory!", 
                        _arena.ActiveTeams.ElementAt(0)._name, 
                        team1.kills,
                        team2.kills));
                }
                else if (team2.kills > team1.kills)
                {
                    team2.win = true;
                    team1.win = false;
                    _arena.sendArenaMessage(String.Format("{0} has won with a {1}-{2} victory!",
                        _arena.ActiveTeams.ElementAt(1)._name,
                        team2.kills,
                        team1.kills));
                }
                else
                {
                    team1.win = false;
                    team2.win = false;
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
                                _arena.sendArenaMessage("Script is tired of counting, ref's take over.");
                                break;
                        }
                    }
                    _arena.sendArenaMessage("Game ended in a draw. Going into OVERTIME!!");
                    _overtime = true;
                    awardMVP = true;
                    return false;
                }

                //First get our list of players saved in our player stats dictionary
                List<Player> players = new List<Player>();
                foreach (KeyValuePair<string, PlayerStats> str in _savedPlayerStats)
                    if (!String.IsNullOrWhiteSpace(str.Key))
                        players.Add(str.Value.player);

                //Now loop through and save everyone's stats
                foreach (Player p in players)
                {
                    int totalPoints = 0;
                    if (_teamStats.ContainsKey(p._team))
                    {
                        _teamStats[p._team].squadID = p._squadID;

                        //Win?
                        if (_teamStats[p._team].win)
                            p.ZoneStat1++;
                        else
                            p.ZoneStat2++;

                        //Count out the total points.
                        totalPoints += (int)p.StatsCurrentGame.Points;

                        //Move on...
                        p.migrateStats();
                        _arena._server._db.updatePlayer(p);
                    }

                    if (!p.IsSpectator)
                        p.spec();
                }

                CS_SquadMatch<InfServer.Data.Database>.SquadStats wStats = new CS_SquadMatch<InfServer.Data.Database>.SquadStats();
                CS_SquadMatch<InfServer.Data.Database>.SquadStats lStats = new CS_SquadMatch<InfServer.Data.Database>.SquadStats();
                if (team1.win)
                {
                    wStats.kills = team1.kills;
                    wStats.deaths = team1.deaths;
                    wStats.points = team1.points;

                    lStats.kills = team2.kills;
                    lStats.deaths = team2.deaths;
                    lStats.points = team2.points;

                    //Report it
                    if (team1.squadID > 0)
                        _arena._server._db.reportMatch(team1.squadID, team2.squadID, wStats, lStats);
                }
                else
                {
                    wStats.kills = team2.kills;
                    wStats.deaths = team2.deaths;
                    wStats.points = team2.points;

                    lStats.kills = team1.kills;
                    lStats.deaths = team1.deaths;
                    lStats.points = team1.points;

                    //Report it
                    if (team2.squadID > 0)
                        _arena._server._db.reportMatch(team2.squadID, team1.squadID, wStats, lStats);
                }

                //_arena.sendArenaMessage("Stats have been recorded. You may leave now.");
                _arena._isMatch = false;
                _overtime = false;
                overtimeType = 0;
                awardMVP = true;
            }
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
                        _savedPlayerStats[p._alias].deaths, p._alias));
                }
            }

            from.sendMessage(0, "Most Deaths");
            IEnumerable<Player> specialPlayers = _arena.Players.OrderByDescending(player => _savedPlayerStats[player._alias].deaths);
            idx = 1; //Only display the top person
            foreach (Player p in specialPlayers)
            {
                if (p == null)
                    continue;

                if (idx-- == 0)
                    break;

                string format = "(D={0}): {1}";
                if (_savedPlayerStats[p._alias] != null)
                {
                    from.sendMessage(0, String.Format(format,
                        _savedPlayerStats[p._alias].deaths,
                        p._alias));
                }
            }

            if (_savedPlayerStats[from._alias] != null)
            {
                string personalFormat = "!Personal Score: (K={0} D={1})";
                from.sendMessage(0, String.Format(personalFormat,
                    _savedPlayerStats[from._alias].kills,
                    _savedPlayerStats[from._alias].deaths));
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
                if (_savedPlayerStats.ContainsKey(killer._alias))
                    _savedPlayerStats[killer._alias].kills++;
                if (_savedPlayerStats.ContainsKey(victim._alias))
                    _savedPlayerStats[victim._alias].deaths++;

                if (_arena._isMatch)
                {
                    _teamStats[killer._team].kills++;
                    _teamStats[victim._team].deaths++;
                }
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
            if (_arena._isMatch && _overtime)
            {
                victim.spec();
                _arena.sendArenaMessage(String.Format("{0} has died out.", victim._alias));
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
                temp.player = player;
                _savedPlayerStats.Add(player._alias, temp);
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
                    player.sendMessage(-1, "Syntax: *mvp alias");
                    return false;
                }

                if (!_arena._isMatch)
                {
                    player.sendMessage(-1, "Can only be used during matches.");
                    return false;
                }

                if (!awardMVP)
                {
                    player.sendMessage(-1, "Cannot award yet till the end of a match.");
                    return false;
                }

                if ((recipient = _arena.getPlayerByName(payload)) == null)
                {
                    player.sendMessage(-1, "Cannot find that player to mvp.");
                    return false;
                }
                recipient.ZoneStat3 += 1;
                _arena.sendArenaMessage("MVP award goes to......... " + recipient._alias);
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
                        player.sendMessage(-1, "*poweradd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
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
                            player.sendMessage(-1, String.Format("*poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
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
                        player.sendMessage(-1, "*powerremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
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
                            player.sendMessage(-1, String.Format("*powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
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
            return false;
        }
		#endregion
	}
}