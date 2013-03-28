using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{
    //THIS WHOLE CODE IS NOT FOR BANNER TRANSFER, leaving it incase we ever find what its really for - Mizz
    /// <summary>
    /// SC_BannerTransfer contains banner information from a particular player
    /// </summary>
    public class SC_BannerTransfer : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 playerID;
        public byte[] bannerData;
        public Int16 test;

        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.BannerTransfer;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_BannerTransfer()
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
            //Write(test);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Send banner data request";
            }
        }
    }
}