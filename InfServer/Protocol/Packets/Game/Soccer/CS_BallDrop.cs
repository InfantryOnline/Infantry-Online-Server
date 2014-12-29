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
    public class CS_BallDrop : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public UInt16 ballID;
        public UInt16 playerID;
        public Int16 velocityX;
        public Int16 velocityY;
        public Int16 velocityZ;
        public Int16 positionX;
        public Int16 positionY;
        public Int16 positionZ;
        public Int16 ballFriction;
        public Int32 tickcount;
        public Boolean scored;

        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.BallDrop;
        static public event Action<CS_BallDrop, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_BallDrop()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_BallDrop(ushort typeID, byte[] buffer, int index, int count)
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
            //22 bytes is the max read
            scored = _contentReader.ReadBoolean();
            ballID = _contentReader.ReadByte();
            velocityX = _contentReader.ReadInt16();
            velocityY = _contentReader.ReadInt16();
            velocityZ = _contentReader.ReadInt16();
            positionX = _contentReader.ReadInt16(); // Confirmed
            positionY = _contentReader.ReadInt16(); // Confirmed
            positionZ = _contentReader.ReadInt16(); // Confirmed
            playerID = _contentReader.ReadUInt16(); // Confirmed (unk0 + unk1)
            ballFriction = _contentReader.ReadInt16(); // Confirmed (unk2 + unk3)
            tickcount = _contentReader.ReadInt32();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player Ball Drop";
            }
        }
    }
}