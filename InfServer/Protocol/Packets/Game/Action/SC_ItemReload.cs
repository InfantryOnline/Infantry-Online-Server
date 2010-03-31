using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ItemReload indicates that an item excecution was successful
	/// </summary>
	public class SC_ItemReload : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int16 itemID;				//ID of item to reload

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ItemReload;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ItemReload()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);
			Write(itemID);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Item reload.";
			}
		}
	}
}
