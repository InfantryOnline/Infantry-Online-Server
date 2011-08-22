using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerCrowns is used to inform which players have crowns or not 
	/// </summary>
	public class SC_PlayerCrowns : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool bCrown;
		public List<short> players;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.SetCrowns;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerCrowns()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(bCrown);
			foreach (short id in players)
				Write(id);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player crown notifications";
			}
		}
	}
}
