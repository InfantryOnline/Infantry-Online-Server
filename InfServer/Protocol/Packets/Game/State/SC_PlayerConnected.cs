using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{
    /// <summary>
    /// SC_PlayerConnected tests to see if a particular player is in the zone.
    /// </summary>
    public class SC_PlayerConnected : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 playerID;

        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.IsZoneConnected;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_PlayerConnected()
            : base(TypeID)
        { }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {	//Type ID
            Write((byte)TypeID);
            //Write(playerID);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player connected request";
            }
        }
    }
}