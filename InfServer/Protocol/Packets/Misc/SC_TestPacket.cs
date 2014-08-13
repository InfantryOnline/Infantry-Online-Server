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
            Write((byte)TypeID);

            //Write(player._id);
            Write((byte)ball._id); // The ID of the ball we're updating
            /*
            Write(ball._state.velocityX);
            Write(ball._state.velocityY);
            Write(ball._state.velocityZ);
            Write(ball._state.positionX); //Confirmed
            Write(ball._state.positionY); //Confirmed
            Write(ball._state.positionZ); //Confirmed
            Write((short)1); //pID
             */
            Skip(13);
            Write((byte)ball._state.unk1);
            Write((byte)0);
            Skip(5);
            /*
            //Write((byte)0); //unk1
            Write((short)1); //unk2
            Write((short)1); //unk3
            Write((short)1); //unk4
            //Write(Environment.TickCount); //unk5
             */
            /*
            Write((byte)unk4);
            Write((byte)unk5);
            Write((byte)unk6);
            unk7 = 1;
            Write((byte)unk7);
             */
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

