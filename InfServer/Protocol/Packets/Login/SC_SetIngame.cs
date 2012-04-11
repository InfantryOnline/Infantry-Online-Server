using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_SetIngame tells the client to start expecting ingame packets
	/// </summary>
	public class SC_SetIngame : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.SetInGame;
        static public event Action<SC_SetIngame, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_SetIngame()
			: base(TypeID)
		{ }


        public SC_SetIngame(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, (Client)_client);
        }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);
		}

        public override void Deserialize()
        {
        }

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Ingame.";
			}
		}
	}
}
