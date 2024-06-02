using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{   /// <summary>
    /// 
    /// </summary>
    public class CS_Unban<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public BanType banType;         //Query type
        public string sender;           //Player requesting
        public string alias;            //Target alias (for retrieving account info)

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.Unban;
        static public event Action<CS_Unban<T>, T> Handlers;

        public enum BanType
        {
            none = 0,
            zone = 1,
            account = 2,
            ip = 3,
            global = 4,
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_Unban()
            : base(TypeID)
        {
            sender = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_Unban(ushort typeID, byte[] buffer, int index, int count)
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
            Write(sender, 0);
            Write(alias, 0);
            Write((byte)banType);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            sender = ReadNullString();
            alias = ReadNullString();
            banType = (BanType)_contentReader.ReadByte();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server unban request";
            }
        }
    }
}
