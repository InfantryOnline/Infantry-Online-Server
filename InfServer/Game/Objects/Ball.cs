using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;

using Assets;

namespace InfServer.Game
{
    // Ball Class
    /// Represents a single ball in an arena
    ///////////////////////////////////////////////////////
    public class Ball : CustomObject, ILocatable
    {	// Member variables
        ///////////////////////////////////////////////////
        public Arena _arena;                //The arena we belong to
        public ushort _id;                  //Our unique identifier
        public Helpers.ObjectState _state;	//The state of our ball
        /// <summary>
        /// Spawning, Picked up or Dropped(-1, 0, 1)
        /// </summary>
        public int ballStatus;              //Whats going on with the ball
        public Player _owner;               //The person holding us
        public Player _lastOwner;           //The person who held us last
        public short ballFriction;
        public uint tickCount;            //Given to us by the client
        public bool deadBall = false;       //Is this ball stuck/unplayabe?

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        #region Member Constructors
        /// <summary>
        /// Generic constructor
        /// </summary>
        public Ball(Player player, short ballID)
        {   //Populate variables
            _id = (ushort)ballID;
            _arena = player._arena;

            _state = new Helpers.ObjectState();
        }

        public Ball(short ballID, Arena arena)
        {	//Populate variables
            _id = (ushort)ballID;
            _arena = arena;

            _state = new Helpers.ObjectState();
        }
        #endregion

        #region ILocatable Functions
        public ushort getID() { return _id; }
        public Helpers.ObjectState getState() { return _state; }
        #endregion

        /// <summary>
        /// Spawns a ball at a desired lio location
        /// </summary>
        static public void Spawn_Ball(Player player, Ball ball)
        {
            Arena arena = ball._arena;
            List<Arena.RelativeObj> valid = new List<Arena.RelativeObj>();
            List<LioInfo.WarpField> warpgroup = arena._server._assets.Lios.getWarpGroupByID(arena._server._zoneConfig.soccer.ballWarpGroup);
            foreach (LioInfo.WarpField warp in warpgroup)
            {
                if (warp.GeneralData.Name.Contains("Ball"))
                {
                    //Do we have the appropriate skills?
                    if (player != null && !InfServer.Logic.Logic_Assets.SkillCheck(player, warp.WarpFieldData.SkillLogic))
                        continue;

                    //Test for viability
                    int playerCount = arena.PlayerCount;

                    if (warp.WarpFieldData.MinPlayerCount > playerCount)
                        continue;

                    if (warp.WarpFieldData.MaxPlayerCount < playerCount)
                        continue;

                    //Specific team warp but on the wrong team
                    if (warp.WarpFieldData.WarpMode == LioInfo.WarpField.WarpMode.SpecificTeam &&
                        player != null && player._team._id != warp.WarpFieldData.WarpModeParameter)
                        continue;

                    List<Arena.RelativeObj> spawnPoints;
                    if (warp.GeneralData.RelativeId != 0)
                    {   //Search for possible points to warp from
                        spawnPoints = arena.findRelativeID(warp.GeneralData.HuntFrequency, warp.GeneralData.RelativeId, player);
                        if (spawnPoints == null)
                            continue;
                    }
                    else
                    {   //Fake it to make it
                        spawnPoints = new List<Arena.RelativeObj> {
                            new Arena.RelativeObj(warp.GeneralData.OffsetX, warp.GeneralData.OffsetY, 0)
                        };
                    }

                    foreach (Arena.RelativeObj point in spawnPoints)
                    {   //Check player concentration
                        playerCount = arena.getPlayersInBox(
                            point.posX, point.posY,
                            warp.GeneralData.Width, warp.GeneralData.Height).Count;

                        if (warp.WarpFieldData.MinPlayersInArea > playerCount)
                            continue;
                        if (warp.WarpFieldData.MaxPlayersInArea < playerCount)
                            continue;

                        point.warp = warp;
                        valid.Add(point);
                    }
                }
            }

            if (valid.Count == 1)
                Spawn_Ball(ball, valid[0].posX, valid[0].posY, valid[0].warp.GeneralData.Width, valid[0].warp.GeneralData.Height);
            else if (valid.Count > 1)
            {
                Arena.RelativeObj obj = valid[arena._rand.Next(0, valid.Count)];
                Spawn_Ball(ball, obj.posX, obj.posY, obj.warp.GeneralData.Width, obj.warp.GeneralData.Height);
            }
            else
            {
                Log.write(TLog.Error, "Cannot satisfy wargroup for ball spawn.");
                return;
            }
        }

        /// <summary>
        /// Spawns a ball at a desired location.
        /// </summary>
        static public void Spawn_Ball(Ball ball, int posX, int posY)
        {
            //Redirect
            Spawn_Ball(ball, posX, posY, posX, posY);
        }

        /// <summary>
        /// Spawns a ball at a desired location
        /// </summary>
        static public void Spawn_Ball(Ball ball, int posX, int posY, int width, int height)
        {
            LvlInfo level = ball._arena._server._assets.Level;
            int x = posX - (level.OffsetX * 16);
            int y = posY - (level.OffsetY * 16);

            short w = (short)(x - width);
            short h = (short)(y - height);
            short top = (short)(x + width);
            short bottom = (short)(y + height);

            ball._state.positionX = (short)(((w - top) / 2) + top);
            ball._state.positionY = (short)(((h - bottom) / 2) + bottom);
            ball._state.positionZ = 1;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;
            ball.ballFriction = -1; //Client figures it out for us
            ball.ballStatus = -1;
            int now = Environment.TickCount;
            ball.tickCount = (uint)now;
            ball._state.lastUpdate = now;
            ball._state.lastUpdateServer = now;

            ball._owner = null;
            ball._lastOwner = null;

            //Lets update
            ball._arena.UpdateBall(ball);

            //Send it
            Helpers.Object_Ball(ball._arena.Players, ball);
        }

        /// <summary>
        /// Removes a ball from the playing field
        /// </summary>
        static public void Remove_Ball(Ball ball)
        {
            //Remove it from our arena
            ball._arena.LostBall(ball);

            //Send it
            Helpers.Object_BallReset(ball._arena.Players, ball);
        }

        /// <summary>
        /// Sends a ball update to all players in the arena
        /// </summary>
        static public void Route_Ball(Ball ball)
        {
            //Update any changes
            ball._arena.UpdateBall(ball);

            //Reroute
            Helpers.Object_Ball(ball._arena.Players, ball);
        }

        /// <summary>
        /// Sends a ball update to all players in the arena
        /// </summary>
        static public void Route_Ball(IEnumerable<Player> targets, Ball ball)
        {
            //Update any changes
            ball._arena.UpdateBall(ball);

            //Reroute
            Helpers.Object_Ball(targets, ball);
        }
    }
}
