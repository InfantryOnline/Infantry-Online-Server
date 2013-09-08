using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_Alias contains alias query packets used with mod commands
    /// </summary>
    public class CS_Alias<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public AliasType aliasType;     //Query type
        public string sender;           //Player requesting
        public string alias;            //Target alias 
        public string aliasTo;          //Recipient
        public int level;               //For mod/de-modding

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.Alias;
        static public event Action<CS_Alias<T>, T> Handlers;

        public enum AliasType
        {
            transfer,
            remove,
            mod,
            dev,
            rename
        }


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_Alias()
            : base(TypeID)
        {
            sender = "";
            alias = "";
            aliasTo = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_Alias(ushort typeID, byte[] buffer, int index, int count)
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
            Write((byte)aliasType);
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
            alias = ReadNullString();
            aliasType = (AliasType)_contentReader.ReadByte();
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
                return "Zone server alias change request";
            }
        }
    }
}
