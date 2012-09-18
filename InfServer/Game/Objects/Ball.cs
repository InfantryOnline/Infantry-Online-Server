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
        }
		#endregion

        public void Route_Ball(IEnumerable<Player> targets)
        {
            foreach (Player player in targets)
            {
                SC_BallState state = new SC_BallState();
                state.positionX = _state.positionX;
                state.positionY = _state.positionY;
                state.positionZ = player._state.positionZ;
                state.ballID = _id;
                if (_state.carrier != null)
                    state.playerID = (short)_state.carrier._id;
                else
                    state.playerID = 0;
                state.TimeStamp = Environment.TickCount;

                //Send it off!
                player._client.send(state);
            }
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
