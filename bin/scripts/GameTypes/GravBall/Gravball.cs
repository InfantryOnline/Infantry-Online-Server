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

        Player assit;
        Player assit2;

        int gameTimerStart; // Tick when the game began
        int gameLength; // Length of game
        int team1Goals; //# of goals for each team
        int team2Goals;
        int _ballID;
        bool _overtime; //True if in overtime
        bool _gameInProgress;
        Player _futureGoal;
        Dictionary<Player, int> queue;
        Dictionary<Player, bool> inPlayers;
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _lastBallCheck;
        //Settings
        private int _minPlayers;				//The minimum amount of players

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

            gameLength = _config.soccer.timer;
            _minPlayers = _config.soccer.minimumPlayers;
            queue = new Dictionary<Player, int>();
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
                _arena.setTicker(1, 1, 3000, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        gameTimerStart = now;
                        _arena.gameStart();
                        _arena.setTicker(1, 1, gameLength * 100, "Time remaining: ", delegate()
                        {	//Trigger the end of game check                            
                            if (team1Goals == team2Goals)
                            {
                                _overtime = true;
                                _arena.setTicker(1, 1, 0, "OVERTIME!!!!!!!!"); // overtime top right                                
                                _arena.sendArenaMessage("Game is tied and going into overtime, next goal wins!");
                            }
                            else
                            {
                                if (!_gameInProgress)
                                    _arena.gameEnd();
                            }
                        });
                    }
                );
            }
            if (now - _tickGameStart > 11000 && now - _lastBallCheck > 100)
            {
                _lastBallCheck = now;
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == _ballID);
                if (ball != null)
                    ball.Route_Ball(_arena.Players);
            }
            return true;
        }

        public int predictGoal()
        {


            return 0;
        }

        #region Events
        /// <summary>
        /// Triggered when a player has dropped the ball
        /// </summary>

        [Scripts.Event("Player.BallDrop")]
        public bool handleBallDrop(Player player, CS_BallDrop pkt)
        {
            //Keep track of assists 
            if (player != null && player != assit)
            {
                assit2 = assit;
                assit = player;
            }
            //Now lets predict if this ball will hit the goal
            Ball ball = _arena._balls.FirstOrDefault(b => b._id == pkt.ballID);
            _ballID = pkt.ballID;
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

                //Check if it is within our goal box
                if (isInsideSquare(p1, p2, p3, p4, ballPoint) || isInsideSquare(p5, p6, p7, p8, ballPoint))
                {//Will be a goal
                    _futureGoal = player;
                    //   _arena.sendArenaMessage("IT COULD BE A GOAL FELLAS");
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
        public bool handleBallPickup(Player player, CS_BallPickup pkt)
        {//Handle saves and pinches and creases and irons and folds
            _ballID = pkt.ballID;
            if (_futureGoal != null)
            {//It is a save or pinch
                if (player._team == _futureGoal._team && player != _futureGoal)
                {//It's a pinch
                    _arena.sendArenaMessage("Goal pinched by " + player._alias);
                }
                else if (player._team != _futureGoal._team && player != _futureGoal)
                {//It's a save
                    _arena.sendArenaMessage("Goal saved by " + player._alias);
                }
            }

            return true;
        }
        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            updateQueue(queue);
        }
        /// <summary>
        /// Called when a goal is scored 
        /// </summary>
        [Scripts.Event("Player.Goal")]
        public bool handlePlayerGoal(Player player, CS_GoalScored pkt)
        {	//We've started!
            //Check for saves/pinches/irons/folds/creases
            if (_futureGoal != null && player._team != _futureGoal._team)
                return true;

            if (player._team == team1)
                team1Goals++;
            else
                team2Goals++;

            //Let everyone know
            if (assit != null && assit2 != null && assit2 != player && assit2._team == player._team)
                _arena.sendArenaMessage("Goal=" + player._alias + "  Team=" + player._team._name + "  assist(" + assit2._alias + ")", _config.soccer.goalBong);
            else
                _arena.sendArenaMessage("Goal=" + player._alias + "  Team=" + player._team._name, _config.soccer.goalBong);

            _arena.sendArenaMessage("SCORE:  " + team1._name + "=" + team1Goals + "  " + team2._name + "=" + team2Goals);
            //See if we need to end the game
            if (!_gameInProgress)
            {
                if (team1Goals > (team2Goals + 2))
                {
                    _gameInProgress = true;
                    _arena.gameEnd();
                }

                else if (team2Goals > (team1Goals + 2))
                {
                    _gameInProgress = true;
                    _arena.gameEnd();
                }
            }
            //Lets create a new ball!
            Ball ball = _arena._balls.FirstOrDefault(b => b._id == pkt.ballID);

            //Initialize its ballstate
            ball._state = new Ball.BallState();

            //Assign default state
            ball._state.positionX = 8757;
            ball._state.positionY = 4824;
            ball._state.positionZ = 5;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;

            ball._state.unk2 = -1;

            _ballID = pkt.ballID;
            //Make each player aware of the ball
            ball.Route_Ball(_arena.Players);

            foreach (Player p in _arena.Players)
            {
                string update = String.Format("{0}: {1} - {2}: {3}", team1._name, team1Goals, team2._name, team2Goals);
                p._arena.setTicker(1, 2, 0, update); // Puts the score top left!
            }


            if (_overtime && !_gameInProgress)
            {//If it was overtime let's end it
                _arena.gameEnd();
                _arena.setTicker(1, 1, 0, null, null); // overtime top right   
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

            inPlayers = new Dictionary<Player, bool>();

            team1Goals = 0;
            team2Goals = 0;

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
            newBall._owner = null;
            newBall._state.carrier = null;
            newBall._state.unk2 = -1;

            //Store it.
            _arena._balls.Add(newBall);

            //Make each player aware of the ball
            newBall.Route_Ball(_arena.Players);
            _ballID = ballID;
            foreach (Player p in _arena.Players)
            {
                inPlayers.Add(p, true);
                p.setVar("Hits", 0);
                p.setEnergy(600);
                p._gotBallID = 999;
                string update = String.Format("{0}: {1} - {2}: {3}", team1._name, 0, team2._name, 0);
                p._arena.setTicker(1, 2, 0, update); // Puts the score top left!
            }
            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _gameInProgress = false;

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
            _victoryTeam = null;
            _overtime = false;

            //Delete the ball at the game end
            if (_ballID != null)
            {
                Ball ball = _arena._balls.FirstOrDefault(b => b._id == _ballID);
                if (ball != null)
                {
                    ball._state.unk2 = 0;
                    ball._owner = null;
                    ball._state.carrier = null;
                    ball.Route_Ball(_arena.Players);
                    _arena._balls.Remove(ball);
                }

            }
            //Find the winning team
            if (team1Goals > team2Goals)
                _victoryTeam = team1;
            else
                _victoryTeam = team2;

            _arena.sendArenaMessage("Game Over");
            _arena.sendArenaMessage(String.Format("&{0} are victorious with a {1}-{2} victory!", _victoryTeam._name, team1Goals, team2Goals));

            team1Goals = 0;
            team2Goals = 0;

            IEnumerable<Player> rankedPlayers;
            int idx;

            rankedPlayers = _arena.PlayersIngame.OrderByDescending(
                p => (p.getVarInt("Hits").Equals(null) ? 0 : p.getVarInt("Hits")));

            idx = 3;	//Only display top three players

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
            _gameInProgress = false;

            _victoryTeam = null;

            return true;
        }
        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            if (_arena.PlayersIngame.Count() == _config.arena.playingMax)
                enqueue(player);

            if (player.getVarInt("Hits").Equals(null))
                player.setVar("Hits", 0);
            return true;
        }

        /// <summary>
        /// Enqueues a player to unspec when there is an opening.
        /// </summary>
        /// <param name="player"></param>
        public void enqueue(Player player)
        {
            if (!queue.ContainsKey(player))
            {
                queue.Add(player, queue.Count());
                player.sendMessage(-1, String.Format("The game is full, (Queue={0})", queue[player]));
            }
            else
            {
                queue.Remove(player);
                player.sendMessage(-1, "Removed from queue");
            }
        }

        public void updateQueue(Dictionary<Player, int> queue)
        {   //Nonsense!
            if (_arena.PlayersIngame.Count() == _config.arena.playingMax)
                return;

            if (queue.Count > 0)
            {

                if (team1.ActivePlayerCount < 8)
                    queue.ElementAt(0).Key.unspec(team1._name);
                else if (team2.ActivePlayerCount < 8)
                    queue.ElementAt(0).Key.unspec(team2._name);

                queue.Remove(queue.ElementAt(0).Key);

                foreach (KeyValuePair<Player, int> player in queue)
                {
                    queue[player.Key] = queue[player.Key] - 1;
                    player.Key.sendMessage(0, String.Format("Queue position is now {0}", queue[player.Key]));
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
                //Initialize its ballstate
                ball._state = new Ball.BallState();

                //Assign default state
                ball._state.positionX = victim._state.positionX;
                ball._state.positionY = victim._state.positionY;
                ball._state.positionZ = victim._state.positionZ;
                ball._state.velocityX = 0;
                ball._state.velocityY = 0;
                ball._state.velocityZ = 0;
                ball._state.unk2 = -1;

                ball.Route_Ball(killer._arena.Players);
            }

            return true;
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
                ball._state.unk2 = -1;

                ball.Route_Ball(player._arena.Players);
            }
            return true;
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


            return true;
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