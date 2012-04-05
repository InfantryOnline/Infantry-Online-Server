using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class CS_Query<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public string alias;                //Whos looking..
        public string recipient;            //Recipient, appended if there is one.
        public QueryType queryType;         //Query type
        public string ipaddress;                    //IPAddress, appended if there is one.

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.Query;
        static public event Action<CS_Query<T>, T> Handlers;


        public enum QueryType
        {
            accountinfo = 01,
            whois = 02,
            aliastransfer = 03,
            find = 04,
            online = 05,
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_Query()
            : base(TypeID)
        {
            alias = "";
            recipient = "";
            ipaddress = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_Query(ushort typeID, byte[] buffer, int index, int count)
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
            Write(alias, 0);
            Write((byte)queryType);

            switch (queryType)
            {
                case QueryType.whois:
                        Write(ipaddress, 0);
                        Write(recipient, 0);
                    break;

                case QueryType.accountinfo:
                    break;

                case QueryType.aliastransfer:
                    break;
            }
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            alias = ReadNullString();
            queryType = (QueryType)_contentReader.ReadByte(); 

            switch (queryType)
            {
                case QueryType.whois:
                        ipaddress = ReadNullString();
                        recipient = ReadNullString();
                    break;

                case QueryType.accountinfo:
                    break;

                case QueryType.aliastransfer:
                    break;
            }
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
