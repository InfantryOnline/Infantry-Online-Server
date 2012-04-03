using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class SC_Whisper<T> : PacketBase
        where T : IClient
    {
        public byte bong;

        public string message;
        public string recipient;
        public string from;


        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.S2C.Whisper;
        static public event Action<SC_Whisper<T>, T> Handlers;

        		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
        public SC_Whisper()
			: base(TypeID)
		{
            message = "";
            recipient = "";
            from = "";
		}

        /// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
        public SC_Whisper(ushort typeID, byte[] buffer, int index, int count)
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
            Write((byte)bong);
            Write(message, 0);
            Write(recipient, 0);
            Write(from, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            bong = _contentReader.ReadByte();
            message = ReadNullString();
            recipient = ReadNullString();
            from = ReadNullString();
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