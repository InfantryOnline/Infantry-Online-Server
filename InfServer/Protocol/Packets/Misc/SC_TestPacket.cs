using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// SC_Banner contains banner information for a particular player
    /// </summary>
    public class SC_TestPacket : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Player player;
        public Ball ball;

        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.BallState;

        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_TestPacket()
            : base(TypeID)
        { }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {	//Type ID
            /*
            Write((byte)((ushort)Helpers.PacketIDs.S2C.RegQuery));
            Write(0);
            string location = "HKEY_LOCAL_MACHINE\\SOFTWARE\\7-Zip\\Path";
            Write(location, location.Length);
             */
            Write((byte)Helpers.PacketIDs.S2C.BallState);
            Write((byte)0);
            Write(player._state.velocityX);
            Write(player._state.velocityY);
            Write(player._state.velocityZ);
            Write((short)(player._state.positionX - 150));
            Write(player._state.positionY);
            Write(player._state.positionZ);
            Write((short)-1);
            Write((short)10);
            Write(Environment.TickCount);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "TestPacket data";
            }
        }
    }
}

