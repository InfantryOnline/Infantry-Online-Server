using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_DamageEvent is used by the client to indicate where a damage event takes place
    /// </summary>
    public class CS_DamageEvent : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 positionX;
        public Int16 positionY;
        public Int16 positionZ;     //What is this?!
        public UInt16 damageID;		//ID of the item which threw the event


        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.DamageEvent;
        static public Action<CS_DamageEvent, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_DamageEvent(ushort typeID, byte[] buffer, int index, int count)
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
            positionX = _contentReader.ReadInt16();
            positionY = _contentReader.ReadInt16();
            positionZ = _contentReader.ReadInt16();
            damageID = _contentReader.ReadUInt16();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player damage event notification";
            }
        }
    }
}