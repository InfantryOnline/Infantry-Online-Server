using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class CS_ChatQuery<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public QueryType queryType;         //Query type
        public string sender;               //Player requesting
        public string payload;              //Query payload

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.ChatQuery;
        static public event Action<CS_ChatQuery<T>, T> Handlers;


        public enum QueryType
        {
            accountinfo,
            whois,
            find,
            online,
            emailupdate,
            zonelist,
            history,
            global,
            ban,
            squadstats,
            helpcall,
            alert,
            modChat,
            deletealias,
            wipe,
            adminlist,
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_ChatQuery()
            : base(TypeID)
        {
            sender = "";
            payload = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_ChatQuery(ushort typeID, byte[] buffer, int index, int count)
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
            Write((byte)queryType);
            Write(payload, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            sender = ReadNullString();
            queryType = (QueryType)_contentReader.ReadByte();
            payload = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server chat query request";
            }
        }
    }
}
