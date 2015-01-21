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
    public class CS_BallPickup : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public UInt16 ballID;
        public Int32 tickcount;
        public Int16 test;
        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.BallPickup;
        static public event Action<CS_BallPickup, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_BallPickup()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_BallPickup(ushort typeID, byte[] buffer, int index, int count)
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
            //6 bytes = max read for this
            ballID = _contentReader.ReadByte();// this is 100% the ballID
            test = _contentReader.ReadByte();
            tickcount = _contentReader.ReadInt32();
            //Log.write(TLog.Warning, "Ball = {0}, test = {1}", ballID.ToString(), test.ToString());
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player Ball Pickup";
            }
        }
    }
}