using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerSetBounty is used to set a player's bounty 
	/// </summary>
	public class SC_PlayerSetBounty : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 playerID;
		public Int16 bounty;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.SetBounty;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerSetBounty()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(playerID);
			Write(bounty);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player set bounty";
			}
		}
	}
}
