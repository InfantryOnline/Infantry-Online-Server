using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerMultiItem is used to prompt the client to enact a multi item 
	/// </summary>
	public class SC_PlayerMultiItem : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public short multiItemID;
		public short itemCount;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.MultiItem;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerMultiItem()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(multiItemID);
			Write(itemCount);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Multi item update";
			}
		}
	}
}
