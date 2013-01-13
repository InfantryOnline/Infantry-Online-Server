using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_VehiclePickup is triggered when a player attempts to pick up a vehicle item
    /// </summary>
    public class CS_VehiclePickup : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public UInt16 vehicleID;	//The id of the vehicle (not type) that we're picking up
        public UInt16 quantity;		//The amount we're picking up

        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.VehiclePickup;
        static public Action<CS_VehiclePickup, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_VehiclePickup(ushort typeID, byte[] buffer, int index, int count)
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
            vehicleID = _contentReader.ReadUInt16();
            quantity = _contentReader.ReadUInt16();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player vehicle pickup";
            }
        }
    }
}