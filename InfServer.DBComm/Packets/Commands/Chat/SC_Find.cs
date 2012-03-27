using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class SC_FindPlayer<T> : PacketBase
        where T : IClient
    {
        public FindResult result;   //Whether or not hes online
        public string findAlias;
        public string alias;        //Whos looking
        public string zone;         //The zone hes in.
        public string arena;        //The arena hes in.


        public enum FindResult
        {
            Success,
            Failure,
        }

        //Packet routing
        public const ushort TypeID = 3;
        static public event Action<SC_FindPlayer<T>, T> Handlers;

        		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
        public SC_FindPlayer()
			: base(TypeID)
		{
            zone = "";
            arena = "";
		}

        /// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
        public SC_FindPlayer(ushort typeID, byte[] buffer, int index, int count)
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
            Write((byte)result);
            Write(findId);
            Write(findAlias, 0);
            Write(alias, 0);
            Write(zone, 0);
            Write(arena, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            result = (FindResult)_contentReader.ReadByte();
            findId = _contentReader.ReadUInt16();
            findAlias = ReadNullString();
            alias = ReadNullString();
            zone = ReadNullString();
            arena = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Database server find reply";
            }
        }
    }
}