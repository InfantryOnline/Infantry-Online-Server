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
    public class Ball : CustomObject
    {	// Member variables
        ///////////////////////////////////////////////////
        public Arena _arena;            //The arena we belong to
        public ushort _id;              //Our unique identifier
        public BallState _state;
        public Player _owner;           //The person holding us
        public Player _lastOwner;       //The person who held us last
        public bool deadBall = false;   //Is this ball stuck/unplayabe?

        ///////////////////////////////////////////////////
        // Member Classes
        ///////////////////////////////////////////////////
        #region Member Classes
        public class BallState
        {
            public bool bPickup { get; set; }
            public short positionX { get; set; }
            public short positionY { get; set; }
            public short positionZ { get; set; }
            public short velocityX { get; set; }
            public short velocityY { get; set; }
            public short velocityZ { get; set; }
            public Player carrier { get; set; }
            public short unk1 { get; set; }
            /// <summary>
            /// Spawning, picked up or dropped? (-1, 0, 1)
            /// </summary>
            public short ballStatus { get; set; }
            public short unk3 { get; set; }
            public short unk4 { get; set; }
            public short unk5 { get; set; }
            public short unk6 { get; set; }
            public short unk7 { get; set; }
            public short inProgress { get; set; }
            public int timeStamp { get; set; }
        }
        #endregion

        /// <summary>
        /// Spawns a ball at a desired lio location
        /// </summary>
        static public void Spawn_Ball(Player player, Ball ball)
        {
            Arena _arena = player._arena;
            List<Arena.RelativeObj> valid = new List<Arena.RelativeObj>();
            List<LioInfo.WarpField> warpgroup = _arena._server._assets.Lios.getWarpGroupByID(_arena._server._zoneConfig.soccer.ballWarpGroup);
            foreach (LioInfo.WarpField warp in warpgroup)
            {
                if (warp.GeneralData.Name.Contains("SoccerBall"))
                {
                    //Do we have the appropriate skills?
                    if (!InfServer.Logic.Logic_Assets.SkillCheck(player, warp.WarpFieldData.SkillLogic))
                        continue;

                    //Test for viability
                    int playerCount = _arena.PlayerCount;

                    if (warp.WarpFieldData.MinPlayerCount > playerCount)
                        continue;

                    if (warp.WarpFieldData.MaxPlayerCount < playerCount)
                        continue;

                    //Specific team warp but on the wrong team
                    if (warp.WarpFieldData.WarpMode == LioInfo.WarpField.WarpMode.SpecificTeam &&
                        player._team._id != warp.WarpFieldData.WarpModeParameter)
                        continue;

                    List<Arena.RelativeObj> spawnPoints;
                    if (warp.GeneralData.RelativeId != 0)
                    {   //Search for possible points to warp from
                        spawnPoints = _arena.findRelativeID(warp.GeneralData.HuntFrequency, warp.GeneralData.RelativeId, player);
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
                        playerCount = _arena.getPlayersInBox(
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
                Arena.RelativeObj obj = valid[_arena._rand.Next(0, valid.Count)];
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

            ball._state.inProgress = 0;
            ball._state.positionX = (short)(((w - top) / 2) + top);
            ball._state.positionY = (short)(((h - bottom) / 2) + bottom);
            ball._state.positionZ = 5;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;
            ball._state.ballStatus = -1;
            ball.deadBall = false;

            SC_BallState state = new SC_BallState();
            state.positionX = ball._state.positionX;
            state.positionY = ball._state.positionY;
            state.positionZ = ball._state.positionZ;
            state.velocityX = ball._state.velocityX;
            state.velocityY = ball._state.velocityY;
            state.velocityZ = ball._state.velocityZ;
            state.ballStatus = ball._state.ballStatus;
            state.unk1 = ball._state.unk1;
            state.unk3 = ball._state.unk3;
            state.unk4 = ball._state.unk4;
            state.unk5 = ball._state.unk5;
            state.unk6 = ball._state.unk6;
            state.unk7 = ball._state.unk7;
            state.ballID = ball._id;
            if (ball._state.carrier != null)
                state.playerID = (short)ball._state.carrier._id;
            else
                state.playerID = 0;
            state.TimeStamp = Environment.TickCount;

            foreach (Player p in ball._arena.Players)
                p._client.sendReliable(state);
        }

        /// <summary>
        /// Removes a ball from the playing field
        /// </summary>
        static public void Remove_Ball(Ball ball)
        {
            ball._state.inProgress = 0;
            SC_BallState state = new SC_BallState();
            state.ballID = ball._id;

            state.positionX = ball._state.positionX;
            state.positionY = ball._state.positionY;
            state.positionZ = ball._state.positionZ;
            state.velocityX = ball._state.velocityX;
            state.velocityY = ball._state.velocityY;
            state.velocityZ = ball._state.velocityZ;
            state.unk1 = -1;
            state.TimeStamp = Environment.TickCount;

            foreach (Player p in ball._arena.Players)
                p._client.sendReliable(state);
        }

        /// <summary>
        /// Sends a ball state packet to all players in the arena
        /// </summary>
        public void Route_Ball(IEnumerable<Player> targets)
        {
            _state.inProgress = 0;
            SC_BallState state = new SC_BallState();
            state.positionX = _state.positionX;
            state.positionY = _state.positionY;
            state.positionZ = _state.positionZ;
            state.velocityX = _state.velocityX;
            state.velocityY = _state.velocityY;
            state.velocityZ = _state.velocityZ;
            state.unk1 = _state.unk1;
            state.ballStatus = _state.ballStatus;
            state.unk3 = _state.unk3;
            state.unk4 = _state.unk4;
            state.unk5 = _state.unk5;
            state.unk6 = _state.unk6;
            state.unk7 = _state.unk7;
            state.ballID = _id;

            if (_state.carrier != null)
                state.playerID = (short)_state.carrier._id;
            else
                state.playerID = 0;
            state.TimeStamp = Environment.TickCount;
             
            foreach (Player player in targets)
                //Send it off!
                player._client.sendReliable(state);
        }

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Generic constructor
        /// </summary>
        public Ball(short ballID, Arena arena, Player player)
        {	//Populate variables
            _id = (ushort)ballID;
            _arena = arena;
            _owner = player;
        }
        public Ball(short ballID, Arena arena)
        {	//Populate variables
            _id = (ushort)ballID;
            _arena = arena;
            _owner = null;
        }
    }
}
