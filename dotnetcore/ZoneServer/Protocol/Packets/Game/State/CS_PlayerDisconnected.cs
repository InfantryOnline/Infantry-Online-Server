using System;
using System.Collections.Generic;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_PlayerZoneDisconnect is sent by the client when a player disconnects from the zone
    /// </summary>
    public class CS_PlayerZoneDisconnected : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////

        Player player = null;

        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.PlayerZoneDisconnected;
        static public Action<CS_PlayerZoneDisconnected, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type.
        /// </summary>
        public CS_PlayerZoneDisconnected(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        { }

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
        {	//Get the information
            player = ((Client<Player>)_client)._obj;
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player Zone Disconnected";
            }
        }
    }
}
