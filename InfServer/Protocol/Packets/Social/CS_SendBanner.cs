using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_SendBanner sends the banner data to a particular player
    /// </summary>
    public class CS_SendBanner : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public UInt16 playerID;     //This is the id its going to
        public byte[] bannerData;

        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.SendBannerTo;
        static public event Action<CS_SendBanner, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_SendBanner(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
        }

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, ((Client<Player>)_client)._obj);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            //Get player id
            playerID = _contentReader.ReadUInt16(); //Player id its going to

            //Read in the banner data
            bannerData = _contentReader.ReadBytes(216 * 2);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Banner sendTo request";
            }
        }
    }
}
