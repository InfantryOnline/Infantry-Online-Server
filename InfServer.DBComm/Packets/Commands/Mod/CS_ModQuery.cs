using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_Alias contains alias query packets used with mod commands
    /// </summary>
    public class CS_ModQuery<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public QueryType queryType;     //Query type
        public string sender;           //Player requesting
        public string query;            //Target alias/command
        public string aliasTo;          //Recipient
        public int level;               //For mod/de-modding

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.ModQuery;
        static public event Action<CS_ModQuery<T>, T> Handlers;

        public enum QueryType
        {
            aliastransfer,
            aliasremove,
            mod,
            dev,
            aliasrename,
            squadtransfer,
            squadremove,
            squadjoin
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_ModQuery()
            : base(TypeID)
        {
            sender = "";
            query = "";
            aliasTo = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_ModQuery(ushort typeID, byte[] buffer, int index, int count)
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
            Write(query, 0);
            Write((byte)queryType);
            Write(aliasTo, 0);
            if (level >= 0)
                Write((Int16)level);
            else
                Skip(2);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            sender = ReadNullString();
            query = ReadNullString();
            queryType = (QueryType)_contentReader.ReadByte();
            aliasTo = ReadNullString();
            level = _contentReader.ReadInt16();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server mod query request";
            }
        }
    }
}
