using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ShopFinished signifies that the current shop transaction is complete
	/// </summary>
	public class SC_ShopFinished : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ShopFinished;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ShopFinished()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);
			Write(true);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Shop confirmation.";
			}
		}
	}
}
