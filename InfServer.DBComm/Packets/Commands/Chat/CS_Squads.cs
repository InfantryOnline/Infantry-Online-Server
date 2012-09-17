using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class CS_Squads<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public QueryType queryType;         //Query type
        public string alias;                //Player requesting
        public string payload;              //Query payload

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.SquadQuery;
        static public event Action<CS_Squads<T>, T> Handlers;


        public enum QueryType
        {
            create,
            invite,
            kick,
            transfer,
            leave,
            online,
            list,
            invitessquad,
            invitesplayer,
            invitesreponse,
            stats,
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_Squads()
            : base(TypeID)
        {
            payload = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_Squads(ushort typeID, byte[] buffer, int index, int count)
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
            Write((byte)queryType);
            Write(alias, 0);
            Write(payload, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            queryType = (QueryType)_contentReader.ReadByte();
            alias = ReadNullString();
            payload = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server query request";
            }
        }
    }
}
