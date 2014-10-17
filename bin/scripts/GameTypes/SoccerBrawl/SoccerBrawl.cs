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

namespace InfServer.Script.GameType_Soccerbrawl
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_Soccerbrawl : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config
        private CfgInfo.SoccerMvp SoccerMvp;    //The zone mvp calculations
        private string FileName;                //This is set by league matches to parse the mvp award into

        private int _tickLastGameUpdate;        //Our queue ticker update
        private int _lastGameCheck;             //The tick at which we last checked for game viability
        private int _tickGameStarting;          //The tick at which the game began starting (0 == not initiated)
        private int _tickGameStarted;           //The tick at which the game started (0 == stopped)
        private int _tickGameNotEnough;         //Updates our ball in game motion even when there arent enough players(1 person practice)
        private int _lastBallCheck;             //Upates our ball in game motion
        private int _lostBallTickerUpdate;      //The tick at which our ball was stuck
        private int _savedTimeStamp;            //When a save was recorded

        //Game Settings
        private int _minPlayers;                //How many players to start the game
        private int _minPlayersToKeepScore;     //How many players needed to save stats
        private int _lostBallInterval = 5;      //How long a ball is glitched before respawning
        private int LeagueSeason = 20;          //Which season are we in?

        //Recordings
        private Team victoryTeam;               //Which team won!
        private int team1Goals;                 //How many goals team 1 has
        private int team2Goals;                 //How many goals team 2 has
        private Team team1;
        private Team team2;
        private Player pass;                    //Who passed the ball
        private Player assist1;                 //Who assisted in a goal
        private Player assist2;                 //Who made the goal
        private Player futureGoal;              //Who could possibly get a goal
        private List<Player> queue;             //Players waiting to play
        private int saveTimeStamp;              //When a save was made
        private double carryTimeStart;          //When a player gets a ball
        private double carryTime;               //The amount the player had the ball
        private bool overtime;                  //Are we in overtime?
        private bool awardMVP = false;          //Are we allowed to award an mvp now?

        //Stats
        private Dictionary<Team, TeamStats> teamStats;
        private Dictionary<Player, PlayerStats> playerStats;

        /// <summary>
        /// Holds our teams current game stats
        /// </summary>
        public class TeamStats
        {
            public long squadID { get; set; }
            public int _kills { get; set; }
            public int _deaths { get; set; }
            public int _goals { get; set; }
            public bool _win { get; set; }
        }

        /// <summary>
        /// Holds our player stats for the current game
        /// </summary>
        public class PlayerStats
        {
            public Data.PlayerStats _currentGame { get; set; }
            public int MVPScore { get; set; }
            public bool _hasPlayed { get; set; }
        }

        //http://stackoverflow.com/questions/14672322/creating-a-point-class-c-sharp
        public class Point
        {
            public int X { get; private set; } //The horizontal location, setting is private
            public int Y { get; private set; } //The vertical location, setting is private
            public Point(int x, int y) //Allow constructors
            {
                X = x; //Set the horizontal location to x (The first argument)
                Y = y; //Set the vertical location to y (The second argument)
            }
        }
        public double triangleArea(Point A, Point B, Point C)
        {
            return (C.X * B.Y - B.X * C.Y) - (C.X * A.Y - A.X * C.Y) + (B.X * A.Y - A.X * B.Y);
        }
        public bool isInsideSquare(Point A, Point B, Point C, Point D, Point P)
        {
            if (triangleArea(A, B, P) > 0 || triangleArea(B, C, P) > 0 || triangleArea(C, D, P) > 0 || triangleArea(D, A, P) > 0)
            {
                return false;
            }
            return true;
        }

        //Handle goal coords here for now
        //Default for big sbl map
        /*
        Point p1 = new Point(135, 1493);
        Point p2 = new Point(240, 1493);
        Point p3 = new Point(240, 1722);
        Point p4 = new Point(135, 1722);

        Point p5 = new Point(5385, 1493);
        Point p6 = new Point(5495, 1493);
        Point p7 = new Point(5495, 1722);
        Point p8 = new Point(5385, 1722);
        */
        //For indoors arena
        Point p1 = new Point(646, 1461);
        Point p2 = new Point(552, 1461);
        Point p3 = new Point(552, 1642);
        Point p4 = new Point(646, 1642);

        Point p5 = new Point(4729, 1461);
        Point p6 = new Point(4823, 1461);
        Point p7 = new Point(4823, 1642);
        Point p8 = new Point(4729, 1642);

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
            SoccerMvp = _config.soccerMvp;
            _arena.playtimeTickerIdx = 0; //Sets the global index for our ticker

            team1 = _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.getTeamByName(_config.teams[1].name);
            team1Goals = 0;
            team2Goals = 0;
            _minPlayers = 2;
            _minPlayersToKeepScore = _config.arena.minimumKeepScorePublic;
            if (_config.soccer.deadBallTimer > _lostBallInterval)
                _lostBallInterval = _config.soccer.deadBallTimer;

            queue = new List<Player>();
            teamStats = new Dictionary<Team, TeamStats>();
            playerStats = new Dictionary<Player, PlayerStats>();

            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {
            int now = Environment.TickCount;

            //Should we check game state yet?
            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;

            _lastGameCheck = now;
            int playing = _arena.PlayerCount;

            //Is game running and do we meet min players?
            if (_arena._bGameRunning && playing < _minPlayers)
                //Stop the game
                _arena.gameEnd();

            //Are we under min players?
            if (playing < _minPlayers)
            {
                //Show the message
                if (!_arena.recycling)
                    _arena.setTicker(1, 0, 0, "Not Enough Players");

                if (queue.Count > 0)
                {
                    queue.Clear();
                    updateTickers();
                }
            }

            //Do we have enough to start a game?
            if (!_arena._bGameRunning && _tickGameStarting == 0 && playing >= _minPlayers)
            {   //Great, Get going!
                _tickGameStarting = now;
                if (!_arena.recycling)
                    _arena.setTicker(1, 0, _config.soccer.startDelay * 100, "Next game: ", delegate()
                    {
                        //Trigger it
                        _arena.gameStart();
                    });
            }

            //Updates our balls(get it!)
            if ((_tickGameStarted > 0 && now - _tickGameStarted > 11000 && now - _lastBallCheck > 100)
                || (!_arena._bGameRunning && now - _tickGameNotEnough > 11000 && now - _lastBallCheck > 100))
            {
                if (_arena._balls.Count() > 0)
                    foreach (Ball ball in _arena._balls.ToList())
                    {
                        //This updates the ball visually
                        ball.Route_Ball(_arena.Players);
                        _lastBallCheck = now;
                        _tickGameNotEnough = now;

                        //Check for a dead ball(non reachable)
                        deadBallTimer(ball);
                    }
            }

            //Updates our scoreboard
            if (_tickGameStarted > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickLastGameUpdate > 1000)
                {
                    updateTickers();
                    _tickLastGameUpdate = now;
                }
            }

            return true;
        }

        #region Game Events
        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {   //We've started!
            int now = Environment.TickCount;
            _tickGameStarted = now;
            _tickGameStarting = 0;

            //Are we recording?
            if (!_arena._bIsPublic)
                _arena._saveStats = _arena._isMatch;

            //Lets reset
            team1Goals = 0;
            team2Goals = 0;
            futureGoal = null;
            pass = null;
            assist1 = null;
            assist2 = null;
            if (awardMVP)
                awardMVP = false;

            //Clear all stats
            playerStats.Clear();
            teamStats.Clear();

            //Clear ball list incase of added balls
            foreach (Ball b in _arena._balls)
                Ball.Remove_Ball(b);
            _arena._balls.Clear();

            team1 = _arena.ActiveTeams.ElementAt(0);
            teamStats.Add(team1, new TeamStats());

            if (_arena.ActiveTeams.Count() > 1)
            {
                team2 = _arena.ActiveTeams.ElementAt(1);
                teamStats.Add(team2, new TeamStats());
            }

            //Make a new id then create a ball
            int ballID = 0;
            Ball newBall = new Ball((short)ballID, _arena);
            //Initialize ball state
            newBall._state = new Ball.BallState();

            //Assign default states
            newBall._state.positionX = 2816;
            newBall._state.positionY = 1600;
            newBall._state.positionZ = 5;
            newBall._state.velocityX = 0;
            newBall._state.velocityY = 0;
            newBall._state.velocityZ = 0;
            newBall._state.ballStatus = -1;
            newBall.deadBall = false;

            //If using lio, lets try searching for a spawn point
            List<LioInfo.WarpField> warpgroup = _arena._server._assets.Lios.getWarpGroupByID(_config.soccer.ballWarpGroup);
            foreach (LioInfo.WarpField warp in warpgroup)
            {
                if (warp.GeneralData.Name.Contains("SoccerBallStart"))
                {
                    newBall._state.positionX = warp.GeneralData.OffsetX;
                    newBall._state.positionY = warp.GeneralData.OffsetY;
                    break;
                }
            }

            //Store it.
            _arena._balls.Add(newBall);

            //Set default ticker
            string update = String.Format("{0}: {1} - {2}: {3}", team1._name, 0, team2._name, 0);
            _arena.setTicker(5, 1, 0, update);

            //Reset variables
            foreach (Player p in _arena.Players)
            {
                p._gotBallID = 999; //No ball in posession
                PlayerStats temp = new PlayerStats();
                temp._currentGame = new Data.PlayerStats();
                temp._hasPlayed = !p.IsSpectator ? true : false;

                if (!playerStats.ContainsKey(p))
                    playerStats.Add(p, temp);
                else
                    playerStats[p] = temp;

                //If league match, get squad id
                if (_arena._isMatch && !String.IsNullOrWhiteSpace(p._squad))
                    if (!p.IsSpectator)
                    {
                        if (teamStats[p._team].squadID == p._squadID)
                            continue;
                        teamStats[p._team].squadID = p._squadID;
                    }
            }

            //Make each player aware of the ball
            newBall.Route_Ball(_arena.Players);

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _arena.setTicker(1, 0, _config.soccer.timer * 100, "Time remaining: ", delegate()
            {
                //Trigger the end of game clock
                if (team1Goals == team2Goals)
                {
                    overtime = true;
                    _arena.setTicker(1, 0, 0, "OVERTIME!!!!!!!");
                    _arena.sendArenaMessage("Game is tied and going into overtime, next goal wins!");
                }
                else
                    _arena.gameEnd();
            });

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {   //Announce it
            _arena.sendArenaMessage("Game Over!", _config.soccer.victoryBong);

            bool record = _tickGameStarted > 0 && _arena.PlayerCount >= _minPlayersToKeepScore;
            if (team1Goals > team2Goals)
            {
                victoryTeam = team1;

                //Are we recording?
                if (record)
                {   //For normal public stat saving
                    foreach (Player p in team1.ActivePlayers)
                        //Give them a win score
                        p.ZoneStat1 += 1;

                    foreach (Player p in team2.ActivePlayers)
                        //Give them a lose score
                        p.ZoneStat2 += 1;
                }

                //Now lets update our player stats table
                Data.PlayerStats temp;
                foreach (KeyValuePair<Player, PlayerStats> pair in playerStats)
                {
                    temp = pair.Value._currentGame;
                    if ((pair.Key._team == team1) || (_arena._isMatch && pair.Key._squadID == teamStats[pair.Key._team].squadID))
                        //Give them a win score
                        temp.zonestat1 += 1;
                    else if ((pair.Key._team == team2) || (_arena._isMatch && pair.Key._squadID == teamStats[pair.Key._team].squadID))
                        //Give them a lose score
                        temp.zonestat2 += 1;

                    int score = (temp.zonestat3 * SoccerMvp.goals) + (temp.zonestat4 * SoccerMvp.assists) + (temp.kills * SoccerMvp.kills) + (temp.zonestat6 * SoccerMvp.steals)
                    + (temp.zonestat7 * SoccerMvp.passes) + (temp.deaths * SoccerMvp.deaths) + (temp.zonestat8 * SoccerMvp.fumbles) + (temp.zonestat5 * SoccerMvp.catches)
                    + ((temp.zonestat9 / 1000) * SoccerMvp.carryTimeFactor) + (temp.zonestat11 * SoccerMvp.saves) + (temp.zonestat12 * SoccerMvp.pinches); //Last one is forced fumbles
                    pair.Value.MVPScore = score;
                }

                if (_arena._isMatch)
                {
                    if (teamStats.ContainsKey(team1))
                        teamStats[team1]._win = true;
                    if (teamStats.ContainsKey(team2))
                        teamStats[team2]._win = false;
                }
            }
            else if (team2Goals > team1Goals)
            {
                victoryTeam = team2;

                //Are we recording?
                if (record)
                {   //For normal public stat saving
                    foreach (Player p in team2.ActivePlayers)
                        //Give them a win score
                        p.ZoneStat1 += 1;

                    foreach (Player p in team1.ActivePlayers)
                        //Give them a lose score
                        p.ZoneStat2 += 1;
                }

                Data.PlayerStats temp;
                foreach (KeyValuePair<Player, PlayerStats> pair in playerStats)
                {
                    temp = pair.Value._currentGame;
                    if ((pair.Key._team == team2) || (_arena._isMatch && pair.Key._squad.Equals(team2._name, StringComparison.OrdinalIgnoreCase)))
                        //Give them a win score
                        temp.zonestat1 += 1;
                    else if ((pair.Key._team == team1) || (_arena._isMatch && pair.Key._squad.Equals(team1._name, StringComparison.OrdinalIgnoreCase)))
                        //Give them a lose score
                        temp.zonestat2 += 1;

                    int score = (temp.zonestat3 * SoccerMvp.goals) + (temp.zonestat4 * SoccerMvp.assists) + (temp.kills * SoccerMvp.kills) + (temp.zonestat6 * SoccerMvp.steals)
                    + (temp.zonestat7 * SoccerMvp.passes) + (temp.deaths * SoccerMvp.deaths) + (temp.zonestat8 * SoccerMvp.fumbles) + (temp.zonestat5 * SoccerMvp.catches)
                    + ((temp.zonestat9 / 1000) * SoccerMvp.carryTimeFactor) + (temp.zonestat11 * SoccerMvp.saves) + (temp.zonestat12 * SoccerMvp.pinches);
                    pair.Value.MVPScore = score;
                }

                if (_arena._isMatch)
                {
                    if (teamStats.ContainsKey(team2))
                        teamStats[team2]._win = true;
                    if (teamStats.ContainsKey(team1))
                        teamStats[team1]._win = false;
                }
            }

            if (victoryTeam == null)
                //No one won
                _arena.sendArenaMessage("&Game ended in a draw. No one wins.");
            else
                _arena.sendArenaMessage(String.Format("&{0} are victorious with a {1} - {2} victory!", victoryTeam._name, team1Goals, team2Goals));

            //Calculate awards
            foreach (Player p in _arena.Players)
            {
                int cash = 0;
                int experience = 0;
                int points = 0;
                if (!p.IsSpectator)
                {
                    cash = p._team == victoryTeam ? 500 : 300;
                    experience = p._team == victoryTeam ? 400 : 200;
                    points = p._team == victoryTeam ? 200 : 100;

                    if (playerStats.ContainsKey(p))
                    {
                        Data.PlayerStats curGame = playerStats[p]._currentGame;
                        int bonus = points + (100 * curGame.zonestat3) + (10 * curGame.zonestat6) + (5 * curGame.zonestat4) + (10 * curGame.zonestat11) + (20 * curGame.zonestat12);
                        points = bonus;
                    }
                }

                p.Cash += cash;
                p.ExperienceTotal += experience;
                p.KillPoints += points;
                p.sendMessage(0, String.Format("!Personal Award: (Cash={0}) (Experience={1}) (Points={2})", cash, experience, points));

                p.syncState();

                if (_arena._isMatch && !p.IsSpectator)
                    p.spec();
            }

            //Are we recording?
            if (_arena._isMatch && _tickGameStarted > 0 && _arena.PlayerCount >= _minPlayersToKeepScore)
            {
                //We are, pass it to our file exporter
                ExportStats();
                awardMVP = true;
            }

            //Reset variables
            _arena.gameReset();
            _arena._isMatch = false;

            return true;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickGameStarted = 0;
            _tickGameStarting = 0;
            team1Goals = 0;
            team2Goals = 0;

            overtime = false;
            victoryTeam = null;
            futureGoal = null;
            pass = null;
            assist1 = null;
            assist2 = null;

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool individualBreakdown(Player from, bool bCurrent)
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
            IEnumerable<Player> rankedPlayers = _arena.Players.OrderByDescending(player => playerStats[player]._currentGame.kills);
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

                if (playerStats[p] != null)
                {
                    from.sendMessage(0, String.Format(format, playerStats[p]._currentGame.kills,
                        playerStats[p]._currentGame.deaths, p._alias));
                }
            }

            //Lets get the top most out of all stats
            string mvp = "", goals = "", assists = "", saves = "", passes = "", catches = "", steals = "", fumbles = "", carrytime = "", pinches = "", ffumbles = "";
            int mvpscore = 0, goal = 0, ass = 0, catched = 0, steal = 0, pass = 0, fumble = 0, carry = 0, pinch = 0, save = 0, ffumble = 0;

            Data.PlayerStats curGame;
            foreach (KeyValuePair<Player, PlayerStats> pair in playerStats)
            {
                curGame = pair.Value._currentGame;
                if (curGame.zonestat3 > goal)
                {
                    goal = curGame.zonestat3;
                    goals = pair.Key._alias;
                }

                if (curGame.zonestat4 > ass)
                {
                    ass = curGame.zonestat4;
                    assists = pair.Key._alias;
                }

                if (curGame.zonestat5 > catched)
                {
                    catched = curGame.zonestat5;
                    catches = pair.Key._alias;
                }

                if (curGame.zonestat6 > steal)
                {
                    steal = curGame.zonestat6;
                    steals = pair.Key._alias;
                }

                if (curGame.zonestat7 > pass)
                {
                    pass = curGame.zonestat7;
                    passes = pair.Key._alias;
                }

                if (curGame.zonestat8 > fumble)
                {
                    fumble = curGame.zonestat8;
                    fumbles = pair.Key._alias;
                }

                if (curGame.zonestat9 > carry)
                {
                    carry = curGame.zonestat9;
                    carrytime = pair.Key._alias;
                }

                if (curGame.zonestat10 > pinch)
                {
                    pinch = curGame.zonestat10;
                    pinches = pair.Key._alias;
                }

                if (curGame.zonestat11 > save)
                {
                    save = curGame.zonestat11;
                    saves = pair.Key._alias;
                }

                if (curGame.zonestat12 > ffumble)
                {
                    ffumble = curGame.zonestat12;
                    ffumbles = pair.Key._alias;
                }

                int score = (goal * SoccerMvp.goals) + (ass * SoccerMvp.assists) + (curGame.kills * SoccerMvp.kills) + (steal * SoccerMvp.steals)
                    + (pass * SoccerMvp.passes) + (curGame.deaths * SoccerMvp.deaths) + (fumble * SoccerMvp.fumbles) + (catched * SoccerMvp.catches)
                    + ((carry / 1000) * SoccerMvp.carryTimeFactor) + (save * SoccerMvp.saves) + (ffumble * SoccerMvp.pinches);
                if (score > mvpscore)
                {
                    mvpscore = score;
                    mvp = pair.Key._alias;
                }
            }

            //Now display each stat
            from.sendMessage(0, String.Format("Highest Mvp Score:    {0}({1})", String.IsNullOrWhiteSpace(mvp) ? from._alias : mvp, mvpscore));
            from.sendMessage(0, String.Format("Most Goals:              {0}({1})", String.IsNullOrWhiteSpace(goals) ? from._alias : goals, goal));
            from.sendMessage(0, String.Format("Most Assists:           {0}({1})", String.IsNullOrWhiteSpace(assists) ? from._alias : assists, ass));
            from.sendMessage(0, String.Format("Most Saves:             {0}({1})", String.IsNullOrWhiteSpace(saves) ? from._alias : saves, save));
            from.sendMessage(0, String.Format("Most Passes:            {0}({1})", String.IsNullOrWhiteSpace(passes) ? from._alias : passes, pass));
            from.sendMessage(0, String.Format("Most Catches:          {0}({1})", String.IsNullOrWhiteSpace(catches) ? from._alias : catches, catched));
            from.sendMessage(0, String.Format("Most Steals:             {0}({1})", String.IsNullOrWhiteSpace(steals) ? from._alias : steals, steal));
            from.sendMessage(0, String.Format("Most Fumbles:           {0}({1})", String.IsNullOrWhiteSpace(fumbles) ? from._alias : fumbles, fumble));
            from.sendMessage(0, String.Format("Most Carry Time:      {0}({1})", String.IsNullOrWhiteSpace(carrytime) ? from._alias : carrytime, carry));
            from.sendMessage(0, String.Format("Most Forced Fumbles: {0}({1})", String.IsNullOrWhiteSpace(ffumbles) ? from._alias : ffumbles, ffumble));

            return true;
        }
        #endregion

        #region Ball Events
        /// <summary>
        /// Triggered when a player has dropped the ball
        /// </summary>
        [Scripts.Event("Player.BallDrop")]
        public bool handleBallDrop(Player player, Ball ball)
        {
            //Keep track of assists
            if (player != null && player != assist1)
            {
                assist2 = assist1;
                assist1 = player;
            }

            //Keep track of passes/catches/fumbles/steals
            pass = player;
            pass._team = player._team;

            //Keep track of carry time
            carryTime = Environment.TickCount - carryTimeStart;
            carryTimeStart = 0;
            if (_tickGameStarted > 0 && _arena.PlayerCount >= _minPlayersToKeepScore)
                //For normal public players
                player.ZoneStat9 += (int)TimeSpan.FromMilliseconds(carryTime).Seconds;

            if (playerStats.ContainsKey(player))
                playerStats[player]._currentGame.zonestat9 += (int)TimeSpan.FromMilliseconds(carryTime).Seconds;

            //Now lets predict if this ball will hit the goal
            double xf = 0;
            double yf = 0;
            double cxi = 0;
            double cyi = 0;
            short xi = ball._state.positionX;
            short yi = ball._state.positionY;

            short dxi = ball._state.velocityX;
            short dyi = ball._state.velocityY;

            double dx, dy;
            dx = dxi;
            dy = dyi;

            for (double i = 0; i < 15; i += 0.0025)
            {   //Find our position at i time after throw
                //applyu friction here
                dx -= dx * 0.001;

                dy -= dy * 0.001;
                xf = xi + (i * dx);
                //xf = xf - (xf * (_config.soccer.defaultFriction / 100));
                //     dyi = dyi - (dyi * (_config.soccer.defaultFriction / 100));

                yf = yi + (i * dy);
                //  yf = yf - (yf * (_config.soccer.defaultFriction / 100));
                Point ballPoint = new Point((int)xf, (int)yf);
                //Find out if we bounce off a wall
                try
                {
                    LvlInfo.Tile tile = _arena._tiles[((int)(yf / 16) * _arena._levelWidth) + (int)(xf / 16)];
                    double xOffset = xf;
                    double yOffset = yf;
                    // _arena.sendArenaMessage("d " + tile.TerrainLookup);
                    if (tile.TerrainLookup != 3 && tile.TerrainLookup != 2 && tile.Blocked)
                    {
                        if (_arena._tiles[((int)(yf / 16) * _arena._levelWidth) + (int)((xf + 25) / 16)].Blocked &&
                            _arena._tiles[((int)(yf / 16) * _arena._levelWidth) + (int)((xf - 25) / 16)].Blocked)
                        {//Horizontal wall
                            dyi *= -1;
                        }
                        else if (_arena._tiles[((int)((yf + 25) / 16) * _arena._levelWidth) + (int)(xf / 16)].Blocked &&
                                _arena._tiles[((int)((yf - 25) / 16) * _arena._levelWidth) + (int)(xf / 16)].Blocked)
                        {//Vertical
                            dxi *= -1;
                        }
                        else if (_arena._tiles[((int)((yf + 25) / 16) * _arena._levelWidth) + (int)((xf + 25) / 16)].Blocked &&
                                _arena._tiles[((int)((yf - 25) / 16) * _arena._levelWidth) + (int)((xf - 25) / 16)].Blocked)
                        {//Positive slope 45 degree
                            short tempx = dxi;
                            dxi = dyi;
                            dyi = tempx;
                        }
                        else if (_arena._tiles[((int)((yf + 25) / 16) * _arena._levelWidth) + (int)((xf - 25) / 16)].Blocked &&
                                _arena._tiles[((int)((yf - 25) / 16) * _arena._levelWidth) + (int)((xf + 25) / 16)].Blocked)
                        {//Negative slope 45 degree
                            short tempx = dxi;
                            dxi = dyi *= -1;
                            dyi = tempx *= -1;
                        }
                        else
                        {//OhShit case                            
                        }
                    }
                }
                catch (Exception)
                {//we are going out of bounds of arena due to no physics and crap
                }

                cxi = xf;
                cyi = yf;

                //Check if it is within our goal box depending on team
                //p1->p4 are left base, p5->p8 are right base
                if (isInsideSquare(p1, p2, p3, p4, ballPoint) || isInsideSquare(p5, p6, p7, p8, ballPoint))
                {//Will be a goal
                    futureGoal = player;
                    break;
                }

                //Not going to be a goal
                futureGoal = null;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player has picked up the ball
        /// </summary>
        [Scripts.Event("Player.BallPickup")]
        public bool handleBallPickup(Player player, Ball ball)
        {
            bool record = _tickGameStarted > 0 && _arena.PlayerCount >= _minPlayersToKeepScore;
            if (futureGoal != null)
            {
                //Is it a save or pinch?
                /* Disabled, sbl is no longer recording this
                if (player._team == futureGoal._team && player != futureGoal)
                {   //Player is on the same team, its a Pinch
                    _arena.sendArenaMessage("Pinch=" + player._alias);

                    //For normal public players
                    if (record)
                        player.ZoneStat10 += 1;

                    if (playerStats.ContainsKey(player))
                        //Save their stat
                        playerStats[player]._currentGame.zonestat10 += 1; //Pinch
                }*/
                if (player._team != futureGoal._team && player != futureGoal)
                {   //Player is from the opposite team, its a Save
                    _arena.sendArenaMessage("Save=" + player._alias);

                    //For normal public players
                    if (record)
                        player.ZoneStat11 += 1;

                    if (playerStats.ContainsKey(player))
                        //Save their stat
                        playerStats[player]._currentGame.zonestat11 += 1; //Save
                }
            }

            //Keep track of passes/catches/steals/fumbles
            if (pass != null && pass != player)
            {
                //Was it a completed pass?
                if (player._team == pass._team)
                {   //Yep! Give them their points

                    //For normal public players
                    if (record)
                    {
                        player.ZoneStat5 += 1;
                        pass.ZoneStat7 += 1;
                    }

                    if (playerStats.ContainsKey(player) && playerStats.ContainsKey(pass))
                    {
                        playerStats[player]._currentGame.zonestat5 += 1; //Catch
                        playerStats[pass]._currentGame.zonestat7 += 1; //Pass
                    }
                }
                else
                {
                    //Ball was stolen
                    if (record)
                    {
                        player.ZoneStat6 += 1;
                        pass.ZoneStat8 += 1;
                    }

                    if (playerStats.ContainsKey(player) && playerStats.ContainsKey(pass))
                    {
                        playerStats[player]._currentGame.zonestat6 += 1; //Steal
                        playerStats[pass]._currentGame.zonestat8 += 1; //Fumble
                    }
                }
            }

            //Reset
            carryTimeStart = Environment.TickCount;
            return true;
        }

        /// <summary>
        /// Called when a goal is scored 
        /// </summary>
        [Scripts.Event("Player.Goal")]
        public bool handlePlayerGoal(Player player, Ball ball)
        {
            //Check for saves/pinches/creases
            if (futureGoal != null && player._team != futureGoal._team)
            {
                futureGoal = null;
                return false;
            }

            //Reset
            futureGoal = null;

            bool record = _tickGameStarted > 0 && _arena.PlayerCount >= _minPlayersToKeepScore;
            if (player._team == team1)
            {
                //Give them a goal
                team1Goals++;
                if (record)
                    //For normal public players
                    player.ZoneStat3 += 1;

                if (teamStats.ContainsKey(team1))
                    teamStats[team1]._goals += 1;
                if (playerStats.ContainsKey(player))
                    playerStats[player]._currentGame.zonestat3 += 1; //Goals
            }
            else
            {
                //Give them a goal
                team2Goals++;
                if (record)
                    //For normal public players
                    player.ZoneStat3 += 1;

                if (teamStats.ContainsKey(team2))
                    teamStats[team2]._goals += 1;
                if (playerStats.ContainsKey(player))
                    playerStats[player]._currentGame.zonestat3 += 1; //Goals
            }

            //Let everyone know
            if (assist2 != null && assist2 != player && assist2._team == player._team)
            {
                _arena.sendArenaMessage(String.Format("Goal={0}  Team={1}  Assist({2})", player._alias, player._team._name, assist2._alias), _config.soccer.goalBong);
                if (record)
                    //For normal public players
                    assist2.ZoneStat4 += 1;

                if (playerStats.ContainsKey(assist2))
                    //Give them an assist
                    playerStats[assist2]._currentGame.zonestat4 += 1; //Assist
            }
            else
                _arena.sendArenaMessage(String.Format("Goal={0}  Team={1}", player._alias, player._team._name), _config.soccer.goalBong);
            //Announce the score
            _arena.sendArenaMessage(String.Format("SCORE:  {0}={1}  {2}={3}", team1._name, team1Goals, team2._name, team2Goals));

            //Was this in overtime?
            if (!overtime)
            {
                //Did we win by 5 or more? (Mercy Rule)
                if (team1Goals > (team2Goals + 4))
                    //End it
                    _arena.gameEnd();
                else if (team2Goals > (team1Goals + 4))
                    //End it
                    _arena.gameEnd();
            }

            //Reset players and scoreboard
            foreach (Player p in _arena.Players)
            {
                if (!p.IsSpectator)
                    Logic_Assets.RunEvent(p, _config.EventInfo.joinTeam);
            }
            updateTickers();

            //Was this score in overtime?
            if (overtime)
            {
                //It was, lets end it
                _arena.gameEnd();
                _arena.setTicker(1, 0, 0, null, null);
            }

            return true;
        }
        #endregion

        #region Player Events
        /// <summary>
        /// Called after leave game(updates the arena player counts first) 
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            if (player._gotBallID != 999)
            {
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == player._gotBallID);
                if (ball == null)
                    return;

                player._gotBallID = 999;
                //Initialize its ballstate
                ball._state = new Ball.BallState();

                //Assign a default state
                ball._state.positionX = player._state.positionX;
                ball._state.positionY = player._state.positionY;
                ball._state.positionZ = player._state.positionZ;
                ball._state.velocityX = 0;
                ball._state.velocityY = 0;
                ball._state.velocityZ = 0;
                ball._state.ballStatus = -1;

                ball.Route_Ball(player._arena.Players);
            }

            //Check to see if we are in the list
            dequeue(player);

            //See if anyone can play
            specInQueue();
        }

        /// <summary>
        /// Triggered when a player wants to spec and leave the game
        /// </summary>
        [Scripts.Event("Player.LeaveGame")]
        public bool playerLeaveGame(Player player)
        {
            if (player._gotBallID != 999)
            {
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == player._gotBallID);
                if (ball == null)
                    return false;

                player._gotBallID = 999;
                //Initialize its ballstate
                ball._state = new Ball.BallState();

                //Assign a default state
                ball._state.positionX = player._state.positionX;
                ball._state.positionY = player._state.positionY;
                ball._state.positionZ = player._state.positionZ;
                ball._state.velocityX = 0;
                ball._state.velocityY = 0;
                ball._state.velocityZ = 0;
                ball._state.ballStatus = -1;

                ball.Route_Ball(player._arena.Players);
            }

            return true;
        }

        /// <summary>
        /// Called when a player leaves the arena
        /// </summary>
        [Scripts.Event("Player.LeaveArena")]
        public void playerLeaveArena(Player player)
        {
            if (player._gotBallID != 999)
            {
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == player._gotBallID);
                if (ball == null)
                    return;

                player._gotBallID = 999;
                //Initialize its ballstate
                ball._state = new Ball.BallState();

                //Assign default state
                ball._state.positionX = player._state.positionX;
                ball._state.positionY = player._state.positionY;
                ball._state.positionZ = player._state.positionZ;
                ball._state.velocityX = 0;
                ball._state.velocityY = 0;
                ball._state.velocityZ = 0;
                ball._state.ballStatus = -1;

                ball.Route_Ball(player._arena.Players);
            }

            //Check to see if we are in the list still
            dequeue(player);

            //Try speccing someone in
            specInQueue();
        }

        /// <summary>
        /// Called when the player joins the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            //Check the queue
            dequeue(player);

            //Add them to the list if not in it
            if (!playerStats.ContainsKey(player))
            {
                playerStats.Add(player, new PlayerStats());
                if (!player.IsSpectator)
                    playerStats[player]._hasPlayed = true;
            }
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            if (_arena.PlayerCount >= _config.arena.playingMax)
            {
                //First time queuer?
                if (!queue.Contains(player))
                    enqueue(player);
                else
                    //Wants to leave the list
                    dequeue(player);

                //Returns false so people arent joined onto a team
                //See scriptArena handlePlayerJoin
                return false;
            }

            //Add them to the list if not in it
            if (!playerStats.ContainsKey(player))
                playerStats.Add(player, new PlayerStats());
            playerStats[player]._hasPlayed = true;

            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            if (!playerStats.ContainsKey(player))
            {
                PlayerStats temp = new PlayerStats();
                temp._currentGame = new Data.PlayerStats();
                temp._hasPlayed = false;
                playerStats.Add(player, temp);
            }
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            if (playerStats.ContainsKey(killer))
                playerStats[killer]._currentGame.kills += 1;
            if (playerStats.ContainsKey(victim))
                playerStats[victim]._currentGame.deaths += 1;

            if (_arena._isMatch)
            {
                if (teamStats.ContainsKey(killer._team))
                    teamStats[killer._team]._kills += 1;
                if (teamStats.ContainsKey(victim._team))
                    teamStats[victim._team]._deaths += 1;
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
            if (victim._gotBallID != 999)
            {
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == victim._gotBallID);
                if (ball == null)
                    return false;

                //Did the victim have the ball?
                if (ball._owner == victim)
                {
                    //For normal public players
                    if (_tickGameStarted > 0 && _arena.PlayerCount >= _minPlayersToKeepScore)
                        killer.ZoneStat12 += 1;

                    if (playerStats.ContainsKey(killer))
                        playerStats[killer]._currentGame.zonestat12 += 1; //Forced Fumble

                    ball._owner = null;
                    ball._lastOwner = victim;
                }
                victim._gotBallID = 999;

                Ball.Spawn_Ball(ball, victim._state.positionX, victim._state.positionY);
            }

            return true;
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("queue"))
            {
                player.sendMessage(0, "Current Queue List:");
                if (queue.Count > 0)
                {
                    int i = 0;
                    //Player wants to see who is waiting
                    foreach (Player P in queue)
                        player.sendMessage(1, (String.Format("{0} - {1}", (++i).ToString(), P._alias)));
                }
                else
                    //Nothing in the list
                    player.sendMessage(0, "Empty.");
            }

            return true;
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
                _arena.sendArenaMessage("MVP award goes to......... ");
                _arena.sendArenaMessage(recipient._alias);
                StreamWriter fs = Logic_File.CreateStatFile(FileName, String.Format("Season {0}", LeagueSeason.ToString()));
                fs.WriteLine("-------------------------------------------------------------------------------------------------------------------------------------------------------------");
                fs.WriteLine(String.Format("MVP = {0}", recipient._alias));
                fs.Close();

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

                Int32.TryParse(args[0].Trim(), out team1Goals);
                Int32.TryParse(args[1].Trim(), out team2Goals);

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

        #region Private Updaters
        /// <summary>
        /// Enqueues a player to unspec when there is an opening.
        /// </summary>
        private void enqueue(Player player)
        {
            if (!queue.Contains(player))
            {
                queue.Add(player);
                //Dont show us as 0 if we are first in list
                int i = queue.IndexOf(player) + 1;
                player.sendMessage(-1, String.Format("The game is full. (Queue={0})", i.ToString()));
            }
        }

        /// <summary>
        /// Dequeues a player from the queue list
        /// </summary>
        private void dequeue(Player player)
        {
            if (queue.Contains(player))
            {
                int index = queue.IndexOf(player) + 1;

                queue.Remove(player);
                player.sendMessage(-1, "Removed from queue.");

                updateQueue(index);
            }
        }

        /// <summary>
        /// Auto spec's in a player if available
        /// </summary>
        private void specInQueue()
        {
            if (queue.Count == 0)
                return;

            if (_arena.PlayerCount < _config.arena.playingMax)
            {
                Player nextPlayer = queue.ElementAt(0);

                //Does the player still exist?
                if ((nextPlayer = _arena.getPlayerByName(nextPlayer._alias)) != null)
                {   //Yep!

                    if (team1.ActivePlayerCount < _config.arena.maxPerFrequency)
                        nextPlayer.unspec(team1);
                    else if (team2.ActivePlayerCount < _config.arena.maxPerFrequency)
                        nextPlayer.unspec(team2);
                    else
                        //Cannot unspec, teams full due to mod powers
                        return;

                    queue.Remove(nextPlayer);
                    updateQueue(1);
                }
            }
        }

        /// <summary>
        /// Updates the queue for any players in it
        /// </summary>
        private void updateQueue(int index)
        {   // Nothing to do here
            if (queue.Count == 0)
                return;

            int i = 1; //Start at 1 because first element in list is 0, we dont show us as 0
            foreach (Player p in queue.ToList())
                //Lets update players
                if (i++ >= index)
                    p.sendMessage(0, String.Format("Queue position is now {0}", i.ToString()));

            updateTickers();
        }

        /// <summary>
        /// Updates scoreboard and possible queue position
        /// </summary>
        private void updateTickers()
        {
            _arena.setTicker(5, 1, 0, delegate(Player P)
            {
                string update = String.Format("{0}: {1} - {2}: {3}", team1._name, team1Goals, team2._name, team2Goals);

                if (P != null)
                    return update;
                return "";
            });

            _arena.setTicker(4, 2, 0, delegate(Player p)
            {
                if (queue.Contains(p))
                {
                    //Dont show us as position 0
                    int i = queue.IndexOf(p) + 1;
                    return String.Format("Queue Position: {0}", i.ToString());
                }
                return "";
            });
        }

        /// <summary>
        /// Respawns ball if unreachable
        /// </summary>
        private void deadBallTimer(Ball ball)
        {
            if (ball == null)
                return;

            short x = ball._state.positionX;
            short y = ball._state.positionY;

            //Are we stuck in/on a wall?
            LvlInfo.Tile tile = _arena._tiles[((int)(y / 16) * _arena._levelWidth) + (int)(x / 16)];
            if (tile.PhysicsVision == 1)
            {
                //Yes, are we still moving though? (Player has us)
                if (ball._state.velocityX == 0 && ball._state.velocityY == 0)
                    return;

                int now = Environment.TickCount;
                //Lets set the timer
                if ((now - _lostBallTickerUpdate) > (_lostBallInterval * 1000) && !ball.deadBall)
                {
                    _lostBallTickerUpdate = now;
                    ball.deadBall = true;
                    _arena.setTicker(5, 3, _lostBallInterval * 100, "Ball Respawning: ", delegate()
                    {
                        //Double check to see if someone used *getball
                        if (!ball.deadBall)
                            return;

                        //Respawn it
                        Ball.Spawn_Ball(ball._owner != null ? ball._owner : ball._lastOwner, ball);
                    });
                }
            }
        }
        #endregion

        #region Stat Exporter
        /// <summary>
        /// Saves all league stats to a file and db
        /// </summary>
        private void ExportStats()
        {   //Sanity Checks
            if (victoryTeam == null)
                return;

            if (playerStats.Count() < 1)
                return;

            if (teamStats.Count() < 1)
                return;

            //Make the file with current date and filename
            DateTime utc = DateTime.Now.ToUniversalTime();
            string filename = String.Format("{0}vs{1} {2}", team1._name, team2._name, utc.ToString());
            StreamWriter fs = Logic_File.CreateStatFile(filename, String.Format("Season {0}", LeagueSeason.ToString()));

            foreach (KeyValuePair<Team, TeamStats> pair in teamStats)
            {
                if (pair.Key == null)
                    continue;

                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Goals = {3}, Win = {4}",
                    pair.Key._name, pair.Value._kills.ToString(), pair.Value._deaths.ToString(), pair.Value._goals.ToString(), pair.Value._win ? "Yes" : "No"));
                fs.WriteLine("-------------------------------------------------------------------");

                foreach (KeyValuePair<Player, PlayerStats> players in playerStats)
                {
                    if (players.Key == null)
                        continue;

                    if (!players.Value._hasPlayed)
                        continue;

                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Points = {2}, Tackles = {3}, Sacked = {4}, Assist Points = {5}, Play Seconds = {6},",
                        players.Key._alias, String.IsNullOrWhiteSpace(players.Key._squad) ? "Yes" : "No", players.Key.KillPoints, players.Key.Kills, players.Key.Deaths, players.Key.AssistPoints, players.Key.PlaySeconds));
                    fs.WriteLine(String.Format("MVP Score = {0}, Goals = {1}, Assists = {2}, Catches = {3}, Steals = {4}, Passes = {5}, Fumbles = {6}, CarryTime = {7}, Saves = {8}, ForcedFumbles = {9}",
                        players.Value.MVPScore, players.Value._currentGame.zonestat3, players.Value._currentGame.zonestat4, players.Value._currentGame.zonestat5, players.Value._currentGame.zonestat6, players.Value._currentGame.zonestat7,
                        players.Value._currentGame.zonestat8, players.Value._currentGame.zonestat9, players.Value._currentGame.zonestat11, players.Value._currentGame.zonestat12));
                    fs.WriteLine();
                }
            }
            //Close it
            fs.Close();

            CS_SquadMatch<Data.Database>.SquadStats win = new CS_SquadMatch<Data.Database>.SquadStats();
            CS_SquadMatch<Data.Database>.SquadStats lose = new CS_SquadMatch<Data.Database>.SquadStats();
            if (teamStats.ContainsKey(victoryTeam))
            {
                if (teamStats[victoryTeam]._win)
                {
                    win.kills = teamStats[victoryTeam]._kills;
                    win.deaths = teamStats[victoryTeam]._deaths;
                    win.points = teamStats[victoryTeam]._goals;

                    if (victoryTeam != team1)
                    {
                        lose.kills = teamStats[team1]._kills;
                        lose.deaths = teamStats[team1]._deaths;
                        lose.points = teamStats[team1]._goals;
                        //Report it
                        _arena._server._db.reportMatch(teamStats[victoryTeam].squadID, teamStats[team1].squadID, win, lose);
                    }
                    else
                    {
                        lose.kills = teamStats[team2]._kills;
                        lose.deaths = teamStats[team2]._deaths;
                        lose.points = teamStats[team2]._goals;
                        //Report it
                        _arena._server._db.reportMatch(teamStats[victoryTeam].squadID, teamStats[team2].squadID, win, lose);
                    }

                    //Let everyone know
                    _arena.sendArenaMessage("!Squad Match has been recorded to the database.");
                }
            }
        }
        #endregion
    }
}
