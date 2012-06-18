using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
    /// SC_PlayerStatsResponse relays a player statistics request to the player
    /// </summary>
    public class SC_ZoneList<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////
        public string requestee;					//The player instance we're referring to

        public List<ZoneInstance> zoneList;            //Our list of zones

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.S2C.ZoneList;
        static public event Action<SC_ZoneList<T>, T> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_ZoneList()
            : base(TypeID)
        { }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public SC_ZoneList(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
            zoneList = new List<ZoneInstance>();
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

            Write(requestee, 0);

            Write(zoneList.Count);
            foreach (ZoneInstance z in zoneList)
            {
                Write(z._name, 0);
                Write(z._ip, 0);
                Write(z._port);
                Write(z._playercount);
            }
        }

        public override void Deserialize()
        {
            requestee = ReadNullString();

            for (int i = 0; i < _contentReader.ReadInt32(); i++)
            {
                zoneList.Add(new ZoneInstance(0,
                    ReadNullString(),
                    ReadNullString(),
                    _contentReader.ReadInt16(),
                    _contentReader.ReadInt32()));
            }
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Player stats response";
            }
        }
    }
}
