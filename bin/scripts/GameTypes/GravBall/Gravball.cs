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


namespace InfServer.Script.GameType_Gravball
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_Gravball : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private Team _victoryTeam;				//The team currently winning!
        private Random _rand;
        Team team1;
        Team team2;
        int gameTimerStart;                     // Tick when the game began
        int team1Goals;
        int team2Goals;
        int bounces;
        double carryTimeStart, carryTime;

        Player pass;                            //Who passed it
        Player assist;
        Player assist2;
        bool _overtime;                         //True if in overtime
        Player _futureGoal;

        List<Player> queue;                     //Our in queued players waiting to play
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickGameNotEnough;         //Only used for active balls without not enough players
        private int _lastBallCheck;             //Updates our ball's in game motion
        private int _tickGameLastTickerUpdate;  //Our Queue ticker update
        private int _lostBallInterval = 5;      //How long a ball is glitched for, default is 5 seconds
        private int _lostBallTickerUpdate;
        //Settings
        private int _minPlayers;				//The minimum amount of players needed to start a game
        private int _minPlayersToKeepScore;     //Min players need to record stats

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
        Point p1 = new Point(15905, 4798);
        Point p2 = new Point(16025, 4798);
        Point p3 = new Point(16025, 4892);
        Point p4 = new Point(15905, 4892);

        Point p5 = new Point(1507, 4804);
        Point p6 = new Point(1615, 4804);
        Point p7 = new Point(1627, 4890);
        Point p8 = new Point(1507, 4890);

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
            _rand = new Random();

            team1 = _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.getTeamByName(_config.teams[1].name);
            team1Goals = 0;
            team2Goals = 0;
            _minPlayers = 2;
            _minPlayersToKeepScore = _config.arena.minimumKeepScorePublic;
            bounces = 0;
            if (_config.soccer.deadBallTimer > _lostBallInterval)
                _lostBallInterval = _config.soccer.deadBallTimer;

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

            //If game is running and we don't have enough players
            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
            {	//Stop the game!
                _arena._bGameRunning = false;
                _arena.setTicker(1, 0, 0, "Not Enough Players");
                _arena.gameReset();

                if (queue.Count > 0)
                {
                    queue.Clear();
                    updateTickers();
                }
            }

           //Do we have enough players to start a game?
            else if (!_arena._bGameRunning && _tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 0, _config.soccer.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        gameTimerStart = now;
                        _arena.gameStart();
                        _arena.setTicker(1, 0, _config.soccer.timer * 100, "Time remaining: ", delegate()
                        {
                            //Trigger the end of game check                            
                            if (team1Goals == team2Goals)
                            {
                                _overtime = true;
                                _arena.setTicker(1, 0, 0, "OVERTIME!!!!!!!!"); // overtime top right                                
                                _arena.sendArenaMessage("Game is tied and going into overtime, next goal wins!");
                            }
                            else
                                _arena.gameEnd();
                        });
                    }
                );
            }

            //Updates our balls(get it!)
            if ((_tickGameStart > 0 && now - _tickGameStart > 11000 && now - _lastBallCheck > 100)
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

        #region Events
        /// <summary>
        /// Triggered when a player has dropped the ball
        /// </summary>
        [Scripts.Event("Player.BallDrop")]
        public bool handleBallDrop(Player player, Ball ball)
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
            if (_tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore)
            {
                carryTime = Environment.TickCount - carryTimeStart;
                carryTimeStart = 0;
                if (_config.zoneStat.name8.Equals("Carry Time"))
                    player.ZoneStat9 += (int)TimeSpan.FromMilliseconds(carryTime).Seconds;
            }

            //Now lets predict if this ball will hit the goal
            double xf = 0;
            double yf = 0;
            double cxi = 0;
            double cyi = 0;
            short xi = ball._state.positionX;
            short yi = ball._state.positionY;

            short dxi = ball._state.velocityX;
            short dyi = ball._state.velocityY;

            for (double i = 0; i < 4; i += 0.0025)
            {//Find our position at i time after throw
                xf = xi + (i * dxi);
                yf = yi + (i * dyi);
                Point ballPoint = new Point((int)xf, (int)yf);
                //Find out if we bounce off a wall
                try
                {
                    LvlInfo.Tile tile = _arena._tiles[((int)(yf / 16) * _arena._levelWidth) + (int)(xf / 16)];
                    double xOffset = xf;
                    double yOffset = yf;
                    if (tile.Blocked)
                    {
                        if (_arena._tiles[((int)(yf / 16) * _arena._levelWidth) + (int)((xf + 25) / 16)].Blocked &&
                            _arena._tiles[((int)(yf / 16) * _arena._levelWidth) + (int)((xf - 25) / 16)].Blocked)
                        {//Horizontal wall
                            dyi *= -1;
                            bounces += 1;
                        }
                        else if (_arena._tiles[((int)((yf + 25) / 16) * _arena._levelWidth) + (int)(xf / 16)].Blocked &&
                                _arena._tiles[((int)((yf - 25) / 16) * _arena._levelWidth) + (int)(xf / 16)].Blocked)
                        {//Vertical
                            dxi *= -1;
                            bounces += 1;
                        }
                        else if (_arena._tiles[((int)((yf + 25) / 16) * _arena._levelWidth) + (int)((xf + 25) / 16)].Blocked &&
                                _arena._tiles[((int)((yf - 25) / 16) * _arena._levelWidth) + (int)((xf - 25) / 16)].Blocked)
                        {//Positive slope 45 degree
                            short tempx = dxi;
                            dxi = dyi;
                            dyi = tempx;
                            bounces += 1;
                        }
                        else if (_arena._tiles[((int)((yf + 25) / 16) * _arena._levelWidth) + (int)((xf - 25) / 16)].Blocked &&
                                _arena._tiles[((int)((yf - 25) / 16) * _arena._levelWidth) + (int)((xf + 25) / 16)].Blocked)
                        {//Negative slope 45 degree
                            short tempx = dxi;
                            dxi = dyi *= -1;
                            dyi = tempx *= -1;
                            bounces += 1;
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

                //Check if it is within our goal box
                if (isInsideSquare(p1, p2, p3, p4, ballPoint) || isInsideSquare(p5, p6, p7, p8, ballPoint))
                {   //Will be a goal
                    _futureGoal = player;
                    break;
                }
                //Not going to be a goal
                _futureGoal = null;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player has dropped the ball
        /// </summary>
        [Scripts.Event("Player.BallPickup")]
        public bool handleBallPickup(Player player, Ball ball)
        {   //Handle saves and pinches and creases and irons and folds
            if (_futureGoal != null)
            {   //It is a save or pinch
                if (player._team == _futureGoal._team && player != _futureGoal)
                {   //It's a pinch
                    _arena.sendArenaMessage("Goal pinched by " + player._alias);
                    if (_tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore
                        && _config.zoneStat.name9.Equals("Pinches")) //Game Progress
                        //Save their stat
                        player.ZoneStat10 += 1; //Pinches
                }
                else if (player._team != _futureGoal._team && player != _futureGoal)
                {   //It's a save
                    _arena.sendArenaMessage("Goal saved by " + player._alias);
                    if (_tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore
                        && _config.zoneStat.name10.Equals("Saves"))
                        //Save their stat
                        player.ZoneStat11 += 1; //Saves
                }
            }

            //Keep track of passes/fumbles/catches/steals
            if (pass != player && _tickGameStart > 0
                && _arena.PlayerCount >= _minPlayersToKeepScore)
            {
                if (pass._team == player._team)
                {
                    //Completed pass, give a point to both
                    player.ZoneStat5 += 1; //Teammate catched it
                    pass.ZoneStat7 += 1; //Pass
                }
                else
                {
                    //Ball was stolen
                    player.ZoneStat6 += 1; //Steal
                    pass.ZoneStat8 += 1; //Fumbled the pass
                }
            }

            carryTimeStart = Environment.TickCount;
            bounces = 0;
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
            if (player._gotBallID != 999)
            {
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == player._gotBallID);

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

            return true;
        }

        /// <summary>
        /// Called when a goal is scored 
        /// </summary>
        [Scripts.Event("Player.Goal")]
        public bool handlePlayerGoal(Player player, CS_GoalScored pkt)
        {	//We've started!
            //Check for saves/pinches/irons/folds/creases
            if (_futureGoal != null && player._team != _futureGoal._team)
            {
                _futureGoal = null;
                return false;
            }

            //Reset
            _futureGoal = null;
            bounces = 0;

            if (player._team == team1)
                team1Goals++;
            else
                team2Goals++;

            //Let everyone know
            if (assist != null && assist2 != null && assist2 != player && assist2._team == player._team)
            {
                _arena.sendArenaMessage("Goal=" + player._alias + "  Team=" + player._team._name + "  assist(" + assist2._alias + ")", _config.soccer.goalBong);
                if (_tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore
                    && _config.zoneStat.name3.Equals("Assists"))
                    assist2.ZoneStat4 += 1; //Assists
            }
            else
                _arena.sendArenaMessage("Goal=" + player._alias + "  Team=" + player._team._name, _config.soccer.goalBong);

            _arena.sendArenaMessage("SCORE:  " + team1._name + "=" + team1Goals + "  " + team2._name + "=" + team2Goals);

            //Save their stat
            if (_tickGameStart > 0 && _arena.PlayerCount >= _minPlayersToKeepScore
                && _config.zoneStat.name2.Equals("Goals"))
                player.ZoneStat3 += 1; //Goals

            //See if we need to end the game
            if (!_overtime)
            {
                //Did we win by 5 or more
                //Mercy Rule
                if (team1Goals > (team2Goals + 2))
                    _arena.gameEnd();
                else if (team2Goals > (team1Goals + 2))
                    _arena.gameEnd();
            }

            foreach (Player p in _arena.Players)
            {
                string update = String.Format("{0}: {1} - {2}: {3}", team1._name, team1Goals, team2._name, team2Goals);
                p._arena.setTicker(4, 1, 0, update); // Puts the score top left!
            }

            if (_overtime)
            {   //If it was overtime let's end it
                _arena.gameEnd();
                _arena.setTicker(1, 0, 0, null, null); // overtime top right   
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

            team1Goals = 0;
            team2Goals = 0;
            _futureGoal = null;
            bounces = 0;

            //Lets clear the list
            _arena._balls.Clear();

            //Grab a new ballID
            int ballID = 0; // First ball, should be 0!          
            //Lets create a new ball!
            Ball newBall = new Ball((short)ballID, _arena);

            //Initialize its ballstate
            newBall._state = new Ball.BallState();

            //Assign default state
            newBall._state.positionX = 8757;
            newBall._state.positionY = 4824;
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

            foreach (Player p in _arena.Players)
            {
                p.setVar("Hits", 0);
                p.setEnergy(600);
                p._gotBallID = 999;
                string update = String.Format("{0}: {1} - {2}: {3}", team1._name, 0, team2._name, 0);
                p._arena.setTicker(1, 2, 0, update); // Puts the score top left!
            }

            //Make everyone aware of the ball
            newBall.Route_Ball(_arena.Players);

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished, perhaps start a new one
            bool record = _arena.PlayerCount >= _minPlayersToKeepScore;
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

            _arena.sendArenaMessage("Game Over!", _config.soccer.victoryBong);
            if (_victoryTeam == null)
                //No one wins
                _arena.sendArenaMessage("&Game ended in a draw. No one wins.");
            else
                _arena.sendArenaMessage(String.Format("&{0} are victorious with a {1}-{2} victory!", _victoryTeam._name, team1Goals, team2Goals));

            IEnumerable<Player> rankedPlayers;
            rankedPlayers = _arena.PlayersIngame.OrderByDescending(
                p => (p.getVarInt("Hits").Equals(null) ? 0 : p.getVarInt("Hits")));

            int idx = 3;	//Only display top three players
            foreach (Player p in rankedPlayers)
            {
                if (idx-- == 0)
                    break;

                if (p.getVarInt("Hits").Equals(null))
                    p.setVar("Hits", 0);

                //Set up the format
                string format = "!3rd - (Hits={0}): {1}";

                switch (idx)
                {
                    case 2:
                        format = "!1st - (Hits={0}): {1}";
                        break;
                    case 1:
                        format = "!2nd - (Hits={0}): {1}";
                        break;
                }

                _arena.sendArenaMessage(String.Format(format, p.getVarInt("Hits"), p._alias));
            }

            foreach (Player p in rankedPlayers)
            {
                int hits = p.getVarInt("Hits");
                int cash = 300 * hits;
                int experience = 200 * hits;
                int points = 100 * hits;
                p.Cash += cash;
                p.KillPoints += points;
                p.ExperienceTotal += experience;
                p.sendMessage(0, String.Format("Personal Award: (Cash={0}) (Experience={1}) (Points={2})", cash, experience, points));
                p.resetVars();
                p.syncState();
                p.clearProjectiles();
            }

            //Shuffle the players up randomly into a new list
            /*
            if (_config.arena.scrambleTeams == 1)
            {
                var random = _rand;
                Player[] shuffledPlayers = _arena.PlayersIngame.ToArray(); //Arrays ftw
                for (int i = shuffledPlayers.Length - 1; i >= 0; i--)
                {
                    int swap = random.Next(i + 1);
                    Player tmp = shuffledPlayers[i];
                    shuffledPlayers[i] = shuffledPlayers[swap];
                    shuffledPlayers[swap] = tmp;
                }

                //Assign the new list of players to teams
                int j = 1;
                foreach (Player p in shuffledPlayers)
                {
                    if (j <= Math.Ceiling((double)shuffledPlayers.Length / 2)) //Team 1 always get the extra player :)
                    {
                        if (p._team != team1) //Only change his team if he's not already on the team d00d
                            team1.addPlayer(p);
                    }
                    else
                    {
                        if (p._team != team2)
                            team2.addPlayer(p);
                    }
                    j++;
                }

                //Notify players of the scramble
                _arena.sendArenaMessage("Teams have been scrambled!");
            }*/

            //Reset variables
            _arena.gameReset();
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
            team1Goals = 0;
            team2Goals = 0;

            _overtime = false;
            _victoryTeam = null;
            _futureGoal = null;

            return true;
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
            if (player.getVarInt("Hits").Equals(null))
                player.setVar("Hits", 0);

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
        /// Enqueues a player to unspec when there is an opening.
        /// </summary>
        public void enqueue(Player player)
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
        public void dequeue(Player player)
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

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            if (killer.getVarInt("Hits").Equals(null))
                killer.setVar("Hits", 1);
            else
                killer.setVar("Hits", killer.getVarInt("Hits") + 1);
            return true;
        }
        #endregion
    }
}