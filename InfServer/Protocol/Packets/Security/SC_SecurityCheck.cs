using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Environment requests a snapshot of the player's environment
	/// </summary>
	public class SC_SecurityCheck : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool bLimitLength;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.SecurityCheck;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
        public SC_SecurityCheck()
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
				return "security request";
			}
		}
	}
}
