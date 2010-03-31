using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerLeave notifies of another player leaving the arena
	/// </summary>
	public class SC_PlayerLeave : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 playerID;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PlayerLeave;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerLeave()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Typeid
			Write((byte)TypeID);

			Write(playerID);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player leaving";
			}
		}
	}
}
