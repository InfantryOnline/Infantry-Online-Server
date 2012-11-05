using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class CS_Ban<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public BanType banType;         //Query type
        public string sender;           //Player requesting
        public string alias;            //Target alias (for retrieving account info)
        public int time;                //Timed ban?
        public string reason;           //Reason for ban

        public uint UID1;
        public uint UID2;
        public uint UID3;

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.Ban;
        static public event Action<CS_Ban<T>, T> Handlers;


        public enum BanType
        {
            global,
            zone,
            arena,
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_Ban()
            : base(TypeID)
        {
            sender = "";
            reason = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_Ban(ushort typeID, byte[] buffer, int index, int count)
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
            Write(time);
            Write(reason, 0);

            Write(UID1);
            Write(UID2);
            Write(UID3);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            sender = ReadNullString();
            alias = ReadNullString();
            banType = (BanType)_contentReader.ReadByte();
            time = _contentReader.ReadInt32();
            reason = ReadNullString();

            UID1 = _contentReader.ReadUInt32();
            UID2 = _contentReader.ReadUInt32();
            UID3 = _contentReader.ReadUInt32();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server ban request";
            }
        }
    }
}
