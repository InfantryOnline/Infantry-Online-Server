using System;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
    /// Chat command logging
    /// </summary>
    public class CS_ChatCommand<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////

        public string sender;
        public string zone;
        public string arena;
        public string reason;

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.ChatCommand;
        static public event Action<CS_ChatCommand<T>, T> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_ChatCommand()
            : base(TypeID)
        {
            sender = "";
            zone = "";
            arena = "";
            reason = "";
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_ChatCommand(ushort typeID, byte[] buffer, int index, int count)
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
            Write(zone, 0);
            Write(arena, 0);
            Write(reason, 0);
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            sender = ReadNullString();
            zone = ReadNullString();
            arena = ReadNullString();
            reason = ReadNullString();
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Zone server chat command logging";
            }
        }
    }
}
