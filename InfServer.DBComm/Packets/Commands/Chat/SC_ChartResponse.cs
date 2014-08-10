using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
    /// SC_Chart relays a chart request to the player
    /// </summary>
    public class SC_ChartResponse<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public string alias;                        //The player's alias

        public CS_ChartQuery<T>.ChartType type;     //The type of chart

        public string title;
        public string columns;
        public string data;

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.S2C.ChartResponse;
        static public event Action<SC_ChartResponse<T>, T> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_ChartResponse()
            : base(TypeID)
        { }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public SC_ChartResponse(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        { }

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

            Write((byte)type);
            Write(title, 0);
            Write(columns, 0);

            Write(data, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            alias = ReadNullString();

            type = (CS_ChartQuery<T>.ChartType)_contentReader.ReadByte();
            title = ReadNullString();
            columns = ReadNullString();

            data = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server chart request";
            }
        }
    }
}