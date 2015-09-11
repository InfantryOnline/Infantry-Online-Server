using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Assets;

namespace InfServer.Game
{
    // Arena Class
    // Represents a single arena in the server
    ///////////////////////////////////////////////////
    public abstract partial class Arena
    {   // Member Variables
        //////////////////////////////////////////////
        protected ObjTracker<Ball> _balls;              //The soccer balls belonging to the arena, indexed by id
        private int _lastUpdateSent = Environment.TickCount;

        // Member Functions
        //////////////////////////////////////////////
        /// <summary>
        /// Looks after every playable ball in the arena
        /// </summary>
        private void pollBalls()
        {
            //Is this a ball type zone?
            if (_balls == null || _balls.Count() == 0)
                return;

            int now = Environment.TickCount;

            //Do we need to send an update?
            if (now - _lastUpdateSent < _server._zoneConfig.soccer.sendTime)
                return;

            foreach (Ball ball in _balls.ToList())
            {
                if (ball.ballStatus == (int)Ball.BallStatus.PickUp)
                    continue;

                //Lets visually update
                Ball.Route_Ball(Players, ball);

                //Check for an unreachable ball
                stuckBall(ball, now);

                //Check for a dead ball(untouched)
                deadBall(ball, now);
            }

            _lastUpdateSent = now;
        }

        /// <summary>
        /// Checks to see if this ball is stuck in/on a wall
        /// </summary>
        private void stuckBall(Ball ball, int tick)
        {   //Exists?
            if (ball == null)
                return;

            short x = ball._state.positionX;
            short y = ball._state.positionY;

            LvlInfo.Tile tile = getTile(x, y);
            if (tile.Vision == 1 || getTerrain(x, y).soccerEnabled == false)
            {
                //Are we being carried or been spawned in?
                if (ball.ballStatus != (int)Ball.BallStatus.Dropped)
                    return;

                if (!ball.deadBall && (tick - ball._state.lastUpdateServer) > 5000) //5 secs
                {
                    ball.deadBall = true;
                    //Update our time
                    ball._state.lastUpdateServer = tick;

                    setTicker(5, 3, 500, "Ball Respawning: ", delegate()
                    {
                        //Make sure someone didnt use *getball
                        if (!ball.deadBall)
                            return;
                        //Respawn it
                        Ball.Spawn_Ball(null, ball);
                    });
                }
            }
        }

        /// <summary>
        /// Checks to see the ball hasn't been touched in awhile
        /// </summary>
        private void deadBall(Ball ball, int tick)
        {   //Exists?
            if (ball == null)
                return;

            int deadballTimer = _server._zoneConfig.soccer.deadBallTimer;
            //Are we even activated?
            if (deadballTimer < 1)
                return;

            //Have we've been picked up?
            if (ball.ballStatus == (int)Ball.BallStatus.PickUp)
                return;

            //Are we timed to be spawned?
            if (ball.deadBall)
                return;

            if ((tick - ball._state.lastUpdateServer) > (deadballTimer * 1000))
            {
                //Are we still moving or been spawned in?
                if (ball.ballStatus != (int)Ball.BallStatus.PickUp)
                    return;

                //Update our tick
                ball._state.lastUpdateServer = tick;

                setTicker(5, 3, deadballTimer * 100, "Ball Respawning: ", delegate()
                {
                    //Double check if someone used *getball on us
                    if (!ball.deadBall)
                        return;
                    //Respawn it
                    Ball.Spawn_Ball(null, ball);
                });
            }
        }

        /// <summary>
        /// Returns a list of balls present in the arena
        /// </summary>
        public IEnumerable<Ball> Balls
        {
            get
            {
                return _balls;
            }
        }

        /// <summary>
        /// Spawns our balls into the arena based on our cfg
        /// </summary>
        public void SpawnBall()
        {
            CfgInfo.Soccer soccer = _server._zoneConfig.soccer;
            int ballCount = soccer.ballCount;
            Ball ball = null;

            //Check our config
            if (soccer.playersPerBall == 0)
            {   //Just spawn all of them
                for (int id = 0; id <= ballCount; id++)
                {
                    ball = newBall((short)id);
                    //Make everyone aware
                    Ball.Spawn_Ball(null, ball);
                }
            }
            else
            {
                int playersPerBall = soccer.playersPerBall;
                //Spawn based on what our cfg wants
                for (int id = 0; id <= PlayersIngame.Count(); id++)
                {
                    if ((id % playersPerBall) == 0)
                    {
                        ball = newBall((short)id);
                        //Make everyone aware
                        Ball.Spawn_Ball(null, ball);
                        if (id == ballCount || id == Arena.maxBalls)
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new ball and adds it to our tracking list
        /// </summary>
        public Ball newBall(short ballID)
        {
            //Maxed out?
            if (_balls.Count == Arena.maxBalls)
                return null;

            //Do we exist?
            if (_balls.getObjByID((ushort)ballID) != null)
                return null;

            //Create our ball object
            Ball ball = new Ball(ballID, this);
            if (ball != null)
            {
                _balls.Add(ball);
                return ball;
            }

            return null;
        }

        /// <summary>
        /// Handles the loss of a ball
        /// </summary>
        public void LostBall(Ball ball)
        {
            if (ball == null)
                return;

            //Let it go
            _balls.Remove(ball);
        }

        /// <summary>
        /// Updates spatial data for the ball
        /// </summary>
        public void UpdateBall(Ball ball)
        {
            if (ball == null)
                return;

            //Do we exist?
            Ball b = _balls.getObjByID(ball._id);
            if (b == null)
                return;

            //Lets update
            _balls.updateObjState(b, b._state);
        }

        /// <summary>
        /// Drops the ball upon death
        /// </summary>
        public void ballHandleDeath(Player victim)
        {   //Reroute
            ballHandleDeath(victim, null);
        }

        /// <summary>
        /// Drops or gives the ball upon death
        /// </summary>
        public void ballHandleDeath(Player victim, Player killer)
        {
            //Is this a ball type zone?
            if (_balls == null || _balls.Count() == 0)
                return;

            //Player?
            if (victim == null)
            {
                Log.write(TLog.Warning, "ballHandleDeath(): Called with null victim.");
                return;
            }

            //Do we even have a ball?
            if (victim._gotBallID == 999)
                return;

            Ball ball = _balls.SingleOrDefault(b => b._id == victim._gotBallID);
            victim._gotBallID = 999;

            if (ball == null)
                return;

            //Did the victim have the ball?
            if (ball._owner == victim)
            {
                ball._owner = null;
                ball._lastOwner = victim;

                //Are we giving it to the killer?
                if (_server._zoneConfig.soccer.killerCatchBall && killer != null)
                {
                    //Pick the ball up
                    ball._state.positionX = killer._state.positionX;
                    ball._state.positionY = killer._state.positionY;
                    ball._state.positionZ = killer._state.positionZ;
                    ball._state.velocityX = 0;
                    ball._state.velocityY = 0;
                    ball._state.velocityZ = 0;
                    ball.deadBall = false;

                    ball._owner = killer;
                    killer._gotBallID = ball._id;

                    //Update spatial data
                    UpdateBall(ball);
                    Ball.Route_Ball(Players, ball); //Send it
                    return;
                }
            }
            //Spawn it otherwise
            Ball.Spawn_Ball(ball, victim._state.positionX, victim._state.positionY);
        }

        /// <summary>
        /// Will drop the ball in place when someone leaves the game or arena
        /// </summary>
        public void onLeaving(Player player)
        {   //Player?
            if (player == null)
            {
                Log.write(TLog.Warning, "onLeaving(): Called with null player.");
                return;
            }

            //Is this a ball type zone?
            if (_balls == null || _balls.Count() == 0)
                return;

            //Do we have a ball?
            if (player._gotBallID == 999)
                return;

            Ball ball = _balls.SingleOrDefault(b => b._id == player._gotBallID);
            player._gotBallID = 999;

            if (ball == null)
                return;

            //Spawn it
            Ball.Spawn_Ball(ball, player._state.positionX, player._state.positionY);
        }
    }
}
