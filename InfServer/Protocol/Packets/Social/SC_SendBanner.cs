using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{
    /// <summary>
    /// SC_SendBanner contains the banner going to a particular player
    /// </summary>
    public class SC_SendBanner : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 playerID;
        public byte[] bannerData;

        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.SendBanner;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_SendBanner()
            : base(TypeID)
        { }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {	//Type ID
            Write((byte)TypeID);
            Write(playerID);

            Write(bannerData);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Sending banner data";
            }
        }
    }
}