﻿using System;
using System.Linq;
using System.Collections.Generic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Protocol;

using Assets;


namespace InfServer.Script.GameType_BoomBall
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_BoomBall : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config
        private CfgInfo.SoccerMvp SoccerMvp;    //The zones mvp calculations

        //Updaters
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickGameLastTickerUpdate;  //Our Queue ticker update
        private int _lastBallCheck;             //Updates our ball's in game motion

        //Game Settings
        private int _minPlayers;                //The minimum amount of players needed to start the game
        private int _minPlayersToKeepScore;     //Min players needed to record stats
        private int _sendBallUpdate;            //When its time to send a ball update packet
        private int _stuckBallInterval = 5;     //How long a ball is glitched for(in seconds)

        //Recordings
        private Team _victoryTeam;				//The team currently winning!
        private List<Player> queue;             //Our in queued players waiting to play
        private Team team1;
        private Team team2;
        private int team1Goals;
        private int team2Goals;
        private double carryTimeStart, carryTime;
        private Player pass;                    //Who passed it
        private Player assist;
        private Player assist2;
        private Player _futureGoal;
        private bool _overtime;                 //True if in overtime
        private int overtimeType;               //Which overtime are we in?


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
            _arena.playtimeTickerIdx = 0; //Sets the global index for our timer

            team1 = _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.getTeamByName(_config.teams[1].name);
            team1Goals = 0;
            team2Goals = 0;
            overtimeType = 0;
            _minPlayers = _config.soccer.minimumPlayers;
            _minPlayersToKeepScore = _config.arena.minimumKeepScorePublic;
            _sendBallUpdate = _config.soccer.sendTime * 10;

            queue = new List<Player>();
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

            //Is the game running and do we meet min players
            if (_arena._bGameRunning && playing < _minPlayers)
                //Stop the game
                _arena.gameEnd();

            //Are we under min players?
            if (playing < _minPlayers)
            {
                _tickGameStarting = 0;
                //Show the message
                if (!_arena.recycling)
                    _arena.setTicker(1, 0, 0, "Not Enough Players");
                _arena.gameReset();

                if (queue.Count > 0)
                {
                    queue.Clear();
                    updateTickers();
                }
            }

            //Do we have enough to start a game yet?
            if (!_arena._bGameRunning && _tickGameStarting == 0 && playing >= _minPlayers)
            {
                //Great, get going!
                _tickGameStarting = now;
                if (!_arena.recycling)
                    _arena.setTicker(1, 0, _config.soccer.startDelay * 100, "Next Game: ", delegate()
                    {
                        //Trigger it!
                        _arena.gameStart();
                    });
            }

            //Updates our balls(get it!)
            if (now - _lastBallCheck > _sendBallUpdate)
            {
                if (_arena.Balls.Count() > 0)
                    foreach (Ball ball in _arena.Balls)
                    {
                        //This updates the ball visually
                        Ball.Route_Ball(ball);
                        _lastBallCheck = now;

                        //Check for a stuck ball(non reachable)
                        stuckBall(ball);

                        //Check for a dead ball(untouched)
                        deadBallTimer(ball);
                    }
            }

            //Updates our queue
            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickGameLastTickerUpdate > 1000)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
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
        {	//We've started!
            int now = Environment.TickCount;
            _tickGameStart = now;
            _tickGameStarting = 0;

            team1Goals = 0;
            team2Goals = 0;
            _futureGoal = null;
            pass = null;
            assist = null;
            assist2 = null;

            team1 = _arena.ActiveTeams.ElementAt(0) != null ? _arena.ActiveTeams.ElementAt(0) : _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.ActiveTeams.Count() > 1 ? _arena.ActiveTeams.ElementAt(1) : _arena.getTeamByName(_config.teams[1].name);

            //Spawn our active balls based on cfg
            SpawnBall();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _arena.setTicker(1, 0, _config.soccer.timer * 100, "Time Remaining: ", delegate()
            {
                //Trigger the end of game
                if (team1Goals == team2Goals)
                {
                    if (_config.soccer.timerOvertime > 0)
                        OverTime();
                    else
                    {
                        _overtime = true;
                        _arena.setTicker(1, 0, 0, "OVERTIME!!!");
                        _arena.sendArenaMessage("Game is tied and going into overtime! Next goal wins!!");
                    }
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
        {	//Announce it!
            _arena.sendArenaMessage("Game Over!", _config.soccer.victoryBong);

            bool record = _tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore;
            if (team1Goals > team2Goals)
            {
                _victoryTeam = team1;
                if (record)
                {
                    foreach (Player p in team1.ActivePlayers)
                        //Give them a win score
                        p.ZoneStat1 += 1;

                    foreach (Player p in team2.ActivePlayers)
                        //Give them a lose score
                        p.ZoneStat2 += 1;
                }
            }
            else if (team2Goals > team1Goals)
            {
                _victoryTeam = team2;
                if (record)
                {
                    foreach (Player p in team2.ActivePlayers)
                        //Give them a win score
                        p.ZoneStat1 += 1;

                    foreach (Player p in team1.ActivePlayers)
                        //Give them a lose score
                        p.ZoneStat2 += 1;
                }
            }

            if (_victoryTeam == null)
                //No one wins
                _arena.sendArenaMessage("&Game ended in a draw. No one wins.");
            else
                _arena.sendArenaMessage(string.Format("&{0} are victorious with a {1}-{2} victory!", _victoryTeam._name, team1Goals, team2Goals));

            //Calculate Awards
            int Multiplier = _arena.PlayerCount * 2;
            foreach (Player p in _arena.Players)
            {
                int cash = 0;
                int exp = 0;
                int points = 0;
                if (!p.IsSpectator)
                {
                    cash = p._team == _victoryTeam ? Multiplier * (_config.soccer.victoryCashReward / 1000) : Multiplier * (_config.soccer.loserCashReward / 1000);
                    exp = p._team == _victoryTeam ? Multiplier * (_config.soccer.victoryExperienceReward / 1000) : Multiplier * (_config.soccer.loserExperienceReward / 1000);
                    points = p._team == _victoryTeam ? Multiplier * (_config.soccer.victoryPointReward / 1000) : Multiplier * (_config.soccer.loserPointReward / 1000);

                    Data.PlayerStats temp = p.StatsCurrentGame;
                    int bonus = points + (100 * temp.zonestat3) + (10 * temp.zonestat6) + (5 * temp.zonestat4) + (10 * temp.zonestat11) + (20 * temp.zonestat12);
                    points = bonus;
                }

                p.Cash += cash;
                p.ExperienceTotal += exp;
                p.KillPoints += points;
                p.sendMessage(0, string.Format("!Personal Award: (Cash={0}) (Experience={1}) (Points={2})", cash, exp, points));

                p.syncState();
            }

            //Reset variables
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
            team1Goals = 0;
            team2Goals = 0;

            _overtime = false;
            overtimeType = 0;
            _victoryTeam = null;
            _futureGoal = null;
            pass = null;
            assist = null;
            assist2 = null;

            return true;
        }

        /// <summary>
        /// Starts overtime or continues it
        /// </summary>
        private void OverTime()
        {
            switch (overtimeType++)
            {
                case 0:
                    _arena.sendArenaMessage("Game is tied and going into overtime!!!");
                    break;
                case 1:
                    _arena.sendArenaMessage("Game is tied and going into double overtime!!!");
                    break;
                case 2:
                    _arena.sendArenaMessage("Game is tied and going into triple overtime!!!");
                    break;
                case 3:
                    _arena.sendArenaMessage("Game is tied and going into quadruple overtime, next goal wins!");
                    break;
            }
            _overtime = true;

            _arena.setTicker(1, 0, _config.soccer.timerOvertime * 100, "OVERTIME: ", delegate()
            {
                //Trigger the end of game clock
                if (team1Goals == team2Goals)
                {
                    if (overtimeType >= 3)
                        _arena.setTicker(1, 0, 0, "Overtime Game Point!");
                    else
                        OverTime();
                }
                else
                    _arena.gameEnd();
            });
        }

        /// <summary>
        /// Called when game ends or player uses ?breakdown
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool individualBreakdown(Player from, bool bCurrent)
        {   //Allows additional "customed" breakdown info
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

                from.sendMessage(0, string.Format(format,
                    t._currentGameKills, t._currentGameDeaths,
                    t._name));
            }

            from.sendMessage(0, "#Individual Statistics Breakdown");
            IEnumerable<Player> rankedPlayers = _arena.Players.OrderByDescending(p => p.StatsCurrentGame.kills);
            idx = 3;	//Only display top three players
            foreach (Player p in rankedPlayers)
            {
                if (p == null)
                    continue;
                if (idx-- == 0)
                    break;

                //Set up the format
                string format = "!3rd - (K={0} D={1}): {2}";
                switch (idx)
                {
                    case 2:
                        format = "!1st - (K={0} D={1}): {2}";
                        break;
                    case 1:
                        format = "!2nd - (K={0} D={1}): {2}";
                        break;
                }
                p.sendMessage(0, string.Format(format, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths, p._alias));
            }

            //Lets get the top most out of all stats
            string mvp = "", goals = "", assists = "", catches = "", steals = "", passes = "", fumbles = "", carrytime = "", saves = "", pinches = "";
            int mvpscore = 0, goal = 0, ass = 0, catched = 0, steal = 0, pass = 0, fumble = 0, carry = 0, save = 0, pinch = 0;

            Data.PlayerStats temp;
            foreach (Player p in _arena.Players)
            {
                temp = p.StatsCurrentGame;
                if (temp.zonestat3 > goal)
                {
                    goal = temp.zonestat3;
                    goals = p._alias;
                }

                if (temp.zonestat4 > ass)
                {
                    ass = temp.zonestat4;
                    assists = p._alias;
                }

                if (temp.zonestat5 > catched)
                {
                    catched = temp.zonestat5;
                    catches = p._alias;
                }

                if (temp.zonestat6 > steal)
                {
                    steal = temp.zonestat6;
                    steals = p._alias;
                }

                if (temp.zonestat7 > pass)
                {
                    pass = temp.zonestat7;
                    passes = p._alias;
                }

                if (temp.zonestat8 > fumble)
                {
                    fumble = temp.zonestat8;
                    fumbles = p._alias;
                }

                if (temp.zonestat9 > carry)
                {
                    carry = temp.zonestat9;
                    carrytime = p._alias;
                }

                if (temp.zonestat11 > save)
                {
                    save = temp.zonestat11;
                    saves = p._alias;
                }

                if (temp.zonestat12 > pinch)
                {
                    pinch = temp.zonestat12;
                    pinches = p._alias;
                }

                int score = (temp.zonestat3 * SoccerMvp.goals) + (temp.zonestat4 * SoccerMvp.assists) + (temp.kills * SoccerMvp.kills) + (temp.zonestat6 * SoccerMvp.steals)
                    + (temp.zonestat7 * SoccerMvp.passes) + (temp.deaths * SoccerMvp.deaths) + (temp.zonestat8 * SoccerMvp.fumbles) + (temp.zonestat5 * SoccerMvp.catches)
                    + ((temp.zonestat9 / 1000) * SoccerMvp.carryTimeFactor) + (temp.zonestat11 * SoccerMvp.saves) + (temp.zonestat12 * SoccerMvp.pinches);
                if (score > mvpscore)
                {
                    mvpscore = score;
                    mvp = p._alias;
                }
            }

            //Now display each stat
            from.sendMessage(0, string.Format("Highest Mvp Score:    {0}({1})", string.IsNullOrWhiteSpace(mvp) ? from._alias : mvp, mvpscore));
            from.sendMessage(0, string.Format("Most Goals:              {0}({1})", string.IsNullOrWhiteSpace(goals) ? from._alias : goals, goal));
            from.sendMessage(0, string.Format("Most Assists:           {0}({1})", string.IsNullOrWhiteSpace(assists) ? from._alias : assists, ass));
            from.sendMessage(0, string.Format("Most Saves:             {0}({1})", string.IsNullOrWhiteSpace(saves) ? from._alias : saves, save));
            from.sendMessage(0, string.Format("Most Passes:            {0}({1})", string.IsNullOrWhiteSpace(passes) ? from._alias : passes, pass));
            from.sendMessage(0, string.Format("Most Catches:          {0}({1})", string.IsNullOrWhiteSpace(catches) ? from._alias : catches, catched));
            from.sendMessage(0, string.Format("Most Steals:             {0}({1})", string.IsNullOrWhiteSpace(steals) ? from._alias : steals, steal));
            from.sendMessage(0, string.Format("Most Fumbles:           {0}({1})", string.IsNullOrWhiteSpace(fumbles) ? from._alias : fumbles, fumble));
            from.sendMessage(0, string.Format("Most Carry Time:      {0}({1})", string.IsNullOrWhiteSpace(carrytime) ? from._alias : carrytime, carry));

            return true;
        }
        #endregion

        #region Ball Events
        /// <summary>
        /// Spawns our balls based on our cfg
        /// Note: we will always spawn a ball even if ballcount = 0
        /// </summary>
        private void SpawnBall()
        {
            int ballCount = _config.soccer.ballCount;
            Ball ball = null;

            //Check our cfg
            if (_config.soccer.playersPerBall == 0)
            {   //Just spawn all of them
                for (int id = 0; id <= ballCount; id++)
                {
                    ball = _arena.newBall((short)id);
                    //Make everyone aware
                    Ball.Spawn_Ball(null, ball);
                }
            }
            else
            {
                int playersPerBall = _config.soccer.playersPerBall;
                //Spawn all balls based on what our cfg wants
                for (int id = 0; id <= _arena.PlayersIngame.Count(); id++)
                {
                    if ((id % playersPerBall) == 0)
                    {
                        ball = _arena.newBall((short)id);
                        //Make everyone aware
                        Ball.Spawn_Ball(null, ball);
                        if (id == ballCount || id == Arena.maxBalls)
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Triggered when a player has dropped the ball
        /// </summary>
        [Scripts.Event("Player.BallDrop")]
        public bool handleBallDrop(Player player, Ball ball, CS_BallDrop drop)
        {
            //Keep track of assists 
            if (player != null && player != assist)
            {
                assist2 = assist;
                assist = player;
            }

            //Keep track of passes/catches/fumbles/steals
            pass = player;
            pass._team = player._team;

            //Keep track of carry time
            carryTime = Environment.TickCount - carryTimeStart;
            carryTimeStart = 0;
            if (_tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore)
                player.ZoneStat9 += TimeSpan.FromMilliseconds(carryTime).Seconds;

            _futureGoal = drop.scoring ? player : null; //If the drop has the potential to score, set the player otherwise set null

            return true;
        }

        /// <summary>
        /// Triggered when a player has dropped the ball
        /// </summary>
        [Scripts.Event("Player.BallPickup")]
        public bool handleBallPickup(Player player, Ball ball)
        {
            bool record = _tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore;

            //Handle saves and pinches and creases and irons and folds
            if (_futureGoal != null)
            {
                //It is a save or pinch
                if (player._team == _futureGoal._team && player != _futureGoal)
                {   //Player is on the same team, It's a pinch
                    _arena.sendArenaMessage("Goal pinched by " + player._alias);
                    if (record)
                        //Save their stat
                        player.ZoneStat12 += 1; //Pinches
                }
                else if (player._team != _futureGoal._team && player != _futureGoal)
                {   //Player is on the opposite team, It's a save
                    _arena.sendArenaMessage("Goal saved by " + player._alias);
                    if (record)
                        //Save their stat
                        player.ZoneStat11 += 1; //Saves
                }
            }

            //Keep track of passes/fumbles/catches/steals
            if (pass != null && pass != player)
            {
                if (pass._team == player._team)
                {
                    //Completed pass, give a point to both
                    if (record)
                    {
                        player.ZoneStat5 += 1; //Teammate catched it
                        pass.ZoneStat7 += 1; //Pass
                    }
                }
                else
                {
                    //Ball was stolen
                    if (record)
                    {
                        player.ZoneStat6 += 1; //Steal
                        pass.ZoneStat8 += 1; //Fumbled the pass
                    }
                }
                //Reset
                pass = null;
            }

            carryTimeStart = Environment.TickCount;
            return true;
        }

        /// <summary>
        /// Called when a goal is scored 
        /// </summary>
        [Scripts.Event("Player.Goal")]
        public bool handlePlayerGoal(Player player, Ball ball, CS_GoalScored pkt)
        {	//We've started!
            //Check for saves/pinches/irons/folds/creases
            if (_futureGoal != null && player._team != _futureGoal._team)
            {
                _futureGoal = null;
                return false;
            }

            //Reset
            _futureGoal = null;

            bool record = _tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore;
            CfgInfo.Terrain terrainID = _arena.getTerrain(pkt.positionX * 16, pkt.positionY * 16);
            if (player._team == team1)
                team1Goals += terrainID.goalPoints;
            else
                team2Goals += terrainID.goalPoints;

            //Let everyone know
            if (assist != null && assist2 != null && assist2 != player && assist2._team == player._team)
            {
                _arena.sendArenaMessage(string.Format("Goal={0}  Team={1}  Assist({2})", player._alias, player._team._name, assist2._alias), _config.soccer.goalBong);
                if (record)
                    assist2.ZoneStat4 += 1; //Assists
            }
            else
                _arena.sendArenaMessage(string.Format("Goal={0}  Team={1}", player._alias, player._team._name), _config.soccer.goalBong);
            //Announce Score
            _arena.sendArenaMessage(string.Format("SCORE:  {0}={1}  {2}={3}", team1._name, team1Goals, team2._name, team2Goals));

            if (record)
                //Give them a goal stat
                player.ZoneStat3 += 1;

            //See if we need to end the game
            if (!_overtime)
            {
                int victoryGoal = _config.soccer.victoryGoals;

                //Did we win by 5 or more? (Mercy Rule)
                if (victoryGoal < 0)
                {
                    if (team1Goals > (team2Goals + (-victoryGoal)))
                        //End it
                        _arena.gameEnd();
                    else if (team2Goals > (team1Goals + (-victoryGoal)))
                        //End it
                        _arena.gameEnd();
                }
                //Did we hit our goal mark?
                else if (victoryGoal > 0)
                {
                    if (team1Goals >= victoryGoal)
                        //End it
                        _arena.gameEnd();
                    else if (team2Goals >= victoryGoal)
                        //End it
                        _arena.gameEnd();
                }
            }

            //Reset scoreboard
            updateTickers();

            //Reset variables
            pass = null;
            assist = null;
            assist2 = null;

            //Was this score in overtime?
            if (_overtime)
            {   //It was, let's end it
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
            //Check to see if we are in the list
            dequeue(player);

            //See if another player can join
            specInQueue();
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
        /// Called when a player leaves the arena
        /// </summary>
        [Scripts.Event("Player.LeaveArena")]
        public void playerLeaveArena(Player player)
        {
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

            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
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
        #endregion

        #region Private Update Calls
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
                player.sendMessage(-1, string.Format("The game is full. (Queue={0})", i.ToString()));
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
        /// Auto specs in a player if available
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
                    p.sendMessage(0, string.Format("Queue position is now {0}", i.ToString()));

            updateTickers();
        }

        /// <summary>
        /// Updates the scoreboard
        /// </summary>
        private void updateTickers()
        {
            _arena.setTicker(5, 1, 0, delegate(Player P)
            {
                string update = string.Format("{0}: {1} - {2}: {3}", team1._name, team1Goals, team2._name, team2Goals);

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
                    return string.Format("Queue Position: {0}", i.ToString());
                }
                return "";
            });
        }

        /// <summary>
        /// Checks to see if a ball is stuck in/on a wall
        /// </summary>
        private void stuckBall(Ball ball)
        {
            if (ball == null)
                return;

            short x = ball._state.positionX;
            short y = ball._state.positionY;

            LvlInfo.Tile tile = _arena.getTile(x, y);
            if (tile.PhysicsVision == 1)
            {
                //Yes, are we moving though? (Player has us)
                if (ball._state.velocityX == 0 && ball._state.velocityY == 0)
                    return;

                int now = Environment.TickCount;
                if (!ball.deadBall && (now - ball._state.lastUpdateServer) > (_stuckBallInterval * 1000))
                {
                    ball.deadBall = true;
                    //Update our time
                    int updateTick = ((now >> 16) << 16) + (ball._state.lastUpdateServer & 0xFFFF);
                    ball._state.lastUpdate = updateTick;
                    ball._state.lastUpdateServer = now;

                    _arena.setTicker(5, 3, _stuckBallInterval * 100, "Ball Respawning: ", delegate()
                    {
                        //Double check to see if someone used *getball
                        if (!ball.deadBall)
                            return;

                        //Respawn it
                        Ball.Spawn_Ball(null, ball);
                    });
                }
            }
        }

        /// <summary>
        /// Checks to see if a ball hasnt been touched in awhile
        /// </summary>
        private void deadBallTimer(Ball ball)
        {
            //Are we even activated?
            if (_config.soccer.deadBallTimer <= 0)
                return;

            //Exist?
            if (ball == null)
                return;

            //Have we been picked up?
            if (ball._owner != null)
                return;

            //Already timed to be warped?
            if (ball.deadBall)
                return;

            int now = Environment.TickCount;
            if ((now - ball._state.lastUpdateServer) > (_config.soccer.deadBallTimer * 1000))
            {
                //Yes, are we still moving with the player or possibly spawned in?
                if (ball._state.velocityX == 0 && ball._state.velocityY == 0)
                    return;

                //Update our time
                int updateTick = ((now >> 16) << 16) + (ball._state.lastUpdateServer & 0xFFFF);
                ball._state.lastUpdate = updateTick;
                ball._state.lastUpdateServer = now;

                _arena.setTicker(5, 3, _config.soccer.deadBallTimer * 100, "Ball Respawning: ", delegate()
                {
                    //Double check to see if someone used *getball
                    if (ball._owner != null)
                    {
                        Log.write(TLog.Warning, "Owner = {0}", ball._owner._alias);
                        return;
                    }

                    //Respawn it
                    Ball.Spawn_Ball(null, ball);
                });
            }
        }
        #endregion

        #region Player Commands
        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            command = (command.ToLower());
            if (command.Equals("queue"))
            {
                player.sendMessage(0, "Current Queue List:");
                if (queue.Count > 0)
                {
                    int i = 0;
                    //Player wants to see who is waiting
                    foreach (Player P in queue)
                        player.sendMessage(1, (string.Format("{0} - {1}", (++i).ToString(), P._alias)));
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
            if (command.Equals("coords"))
                player.sendMessage(0, string.Format("{0},{1}", player._state.positionX, player._state.positionY));

            if (command.Equals("setscore"))
            {
                if (string.IsNullOrEmpty(payload))
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
                    if (!string.IsNullOrWhiteSpace(payload))
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
                        recipient.sendMessage(0, string.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, string.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = true;
                        recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                        recipient.sendMessage(0, string.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, string.Format("You have promoted {0} to level {1}.", recipient._alias, level));
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
                    if (string.IsNullOrEmpty(payload))
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
                            player.sendMessage(-1, string.Format("*poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            player.sendMessage(0, "Note: there can be only 1 admin level.");
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, string.Format("You have promoted {0} to level {1}.", payload, level));
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
                        recipient.sendMessage(0, string.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
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
                    if (!string.IsNullOrWhiteSpace(payload))
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
                        recipient.sendMessage(0, string.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, string.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = false;
                        recipient._permissionStatic = Data.PlayerPermission.Normal;
                        recipient.sendMessage(0, string.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, string.Format("You have demoted {0} to level {1}.", recipient._alias, level));
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
                    if (string.IsNullOrEmpty(payload))
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
                            player.sendMessage(-1, string.Format("*powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, string.Format("You have demoted {0} to level {1}.", payload, level));
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
                        recipient.sendMessage(0, string.Format("You have been depowered to level {0}.", level));
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