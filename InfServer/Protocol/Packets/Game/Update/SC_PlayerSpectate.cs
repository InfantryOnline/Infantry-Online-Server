using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerSpectate is used to force the client to spectate a certain player 
	/// </summary>
	public class SC_PlayerSpectate : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 spectatorID;
		public UInt16 playerID;
		public Int32 unk1;
		public Int32 unk2;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PlayerSpectate;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerSpectate()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(spectatorID);
			Write(playerID);
			Write(unk1);
			Write(unk2);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player spectate notification";
			}
		}
	}
}
