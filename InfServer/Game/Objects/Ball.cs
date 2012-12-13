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
        public Arena _arena;	    //The arena we belong to
        public ushort _id;           //Our unique identifier
        public BallState _state;

        ///////////////////////////////////////////////////
        // Member Classes
        ///////////////////////////////////////////////////
        #region Member Classes
        public class BallState
        {
            public short positionX { get; set; }
            public short positionY { get; set; }
            public short positionZ { get; set; }
            public short velocityX { get; set; }
            public short velocityY { get; set; }
            public short velocityZ { get; set; }
            public Player carrier { get; set; }
            public short unk1 { get; set; }
            public short unk2 { get; set; }
            public short unk3 { get; set; }
            public short unk4 { get; set; }
            public short unk5 { get; set; }
            public short unk6 { get; set; }
            public short unk7 { get; set; }
            public short delete { get; set; }
            public short inProgress { get; set; }

        }
        #endregion

        public void Route_Ball(IEnumerable<Player> targets)
        {
            _state.inProgress = 1;
            foreach (Player player in targets)
            {
                SC_BallState state = new SC_BallState();
                state.positionX = _state.positionX;
                state.positionY = _state.positionY;
                state.positionZ = _state.positionZ;
                state.velocityX = _state.velocityX;
                state.velocityY = _state.velocityY;
                state.velocityZ = _state.velocityZ;
                state.unk1 = _state.unk1;
                state.unk2 = _state.unk2;
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
                //state.delete = _state.delete;
                //Send it off!
                player._client.send(state);
                Log.write(String.Format("ball state update for player {0}", player._id));

            }
            _state.inProgress = 0;
        }

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Generic constructor
        /// </summary>
        public Ball(short ballID, Arena arena)
        {	//Populate variables
            _id = (ushort)ballID;
            _arena = arena;
        }
    }
}
