using System;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// Zone update packet used between server connections
    /// </summary>
    public class CS_ZoneUpdate<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////

        public string zoneName;
        public string zoneDescription;
        public string zoneIP;
        public int zonePort;
        public bool zoneIsAdvanced;
        public short zoneActive;

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.ZoneUpdate;
        static public event Action<CS_ZoneUpdate<T>, T> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_ZoneUpdate()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_ZoneUpdate(ushort typeID, byte[] buffer, int index, int count)
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

            Write(zoneName, 0);
            Write(zoneDescription, 0);
            Write(zoneIP, 0);
            Write(zonePort);
            Write(zoneIsAdvanced);
            Write(zoneActive);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            zoneName = ReadNullString();
            zoneDescription = ReadNullString();
            zoneIP = ReadNullString();
            zonePort = _contentReader.ReadInt32();
            zoneIsAdvanced = _contentReader.ReadBoolean();
            zoneActive = _contentReader.ReadInt16();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Database zone update packet";
            }
        }
    }
}
