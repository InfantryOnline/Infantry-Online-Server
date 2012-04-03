using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class SC_PrivateChat<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////

        public string chat;
        public string message;
        public string from;
        public string users;

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.S2C.PrivateChat;
        static public event Action<SC_PrivateChat<T>, T> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_PrivateChat()
            : base(TypeID)
        {
            message = "";
            chat = "";
            from = "";
            users = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public SC_PrivateChat(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
        }

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, (_client as Client<T>)._obj);
        }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {	//Type ID
            Write((byte)TypeID);
            Write(chat, 0);
            Write(message, 0);
            Write(from, 0);
            Write(users, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            chat = ReadNullString();
            message = ReadNullString();
            from = ReadNullString();
            users = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server chat route";
            }
        }
    }
}
