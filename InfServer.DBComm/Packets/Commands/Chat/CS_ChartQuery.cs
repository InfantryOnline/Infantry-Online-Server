using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{
    /// <summary>
    /// CS_Chart requests a chart from the game server
    /// </summary>
    public class CS_ChartQuery<T> : PacketBase
        where T : IClient
    {   //Member Variables
        /////////////////////////////////

        public string alias;                //The player's alias
        public ChartType type;              //The type of request
        public string title;                //Title displayed at the top
        public string columns;              //The set columns

        /// <summary>
        /// What type of chart requested
        /// </summary>
        public enum ChartType
        {
            chatchart = 0
        }

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.ChartQuery;
        static public event Action<CS_ChartQuery<T>, T> Handlers;

        /////////////////////////////////
        // Member Functions
        /////////////////////////////////
        /// <summary>
        /// Creates an empty packet of a specified type. Used to construct
        /// a new packet for sending.
        /// </summary>
        public CS_ChartQuery()
            : base(TypeID)
        { }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_ChartQuery(ushort typeID, byte[] buffer, int index, int count)
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
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            alias = ReadNullString();

            type = (ChartType)_contentReader.ReadByte();
            title = ReadNullString();
            columns = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player chart request";
            }
        }
    }
}
