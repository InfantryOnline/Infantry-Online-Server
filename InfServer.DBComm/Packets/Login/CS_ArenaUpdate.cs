using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{   /// <summary>
    /// CS_ArenaUpdate relays a player joining an arena to the database server
    /// </summary>
    public class CS_ArenaUpdate<T> : PacketBase
        where T : IClient
    {   //Member variables
        //////////////////////////////////////////
        public PlayerInstance player;       //The player instance we're referring to
        public string arena;                //The arena we joined/left

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.ArenaUpdate;
        static public event Action<CS_ArenaUpdate<T>, T> Handlers;

        //////////////////////////////////////////
        //Member Functios
        //////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of a specificed type. This is used for
        /// constructing a new packet for sending.
        /// </summary>
        public CS_ArenaUpdate()
            : base(TypeID)
        {
            player = new PlayerInstance();
        }

        /// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_ArenaUpdate(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
			player = new PlayerInstance();
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

			Write(player.id);
			Write(player.magic);

            Write(arena, 0);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			player.id = _contentReader.ReadUInt16();
			player.magic = _contentReader.ReadInt32();

            arena = ReadNullString();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Arena join/leave notification";
			}
		}
	}
}