using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_PlayerUpdate contains player statistical information for updating
    /// </summary>
    public class CS_GoalScored : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public UInt16 ballID;
        public Int16 positionX;
        public Int16 positionY;
        public Int32 tickcount;

        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.GoalScored;
        static public event Action<CS_GoalScored, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_GoalScored()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_GoalScored(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
        }

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, (_client as Client<Player>)._obj);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            //This packet is max of 9 bytes
            ballID = _contentReader.ReadByte(); // Ball id
            tickcount = _contentReader.ReadInt32(); //Time it happened
            positionX = _contentReader.ReadInt16(); //Where the ball crosses the line
            positionY = _contentReader.ReadInt16(); //Where the ball crosses the line
        }

        /// <summary>
        /// Returns a meaning of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player Goal";
            }
        }
    }
}