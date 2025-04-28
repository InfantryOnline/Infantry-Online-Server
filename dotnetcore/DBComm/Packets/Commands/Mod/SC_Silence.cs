﻿using InfServer.Network;
using InfServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfServer.Protocol
{
    public class SC_Silence<T> : PacketBase
        where T : IClient
    {
        public string alias { get; set; }

        public int minutes { get; set; }

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.S2C.GlobalSilence;
        static public event Action<SC_Silence<T>, T> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_Silence()
            : base(TypeID)
        {

        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public SC_Silence(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
        }

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {   //Call all handlers!
            if (Handlers != null)
                Handlers(this, (_client as Client<T>)._obj);
        }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {   //Type ID
            Write((byte)TypeID);

            Write(alias, 0);
            Write(minutes);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            alias = ReadNullString();
            minutes = _contentReader.ReadInt32();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Database server global silence";
            }
        }
    }
}
