using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_EnterArena signifies that the server has finished sending
	/// the game state, and the client is ready to join the arena.
	/// </summary>
	public class SC_EnterArena : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public const ushort TypeID = (ushort)(ushort)Helpers.PacketIDs.S2C.EnterArena;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_EnterArena()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Enter arena.";
			}
		}
	}
}
