using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ItemDrop is used to update an itemdrop in the client 
	/// </summary>
	public class SC_ItemDrop : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 dropID;		//ID of the drop
		public UInt16 quantity;	

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ItemDrop;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ItemDrop()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(dropID);
			Write(quantity);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Item drop destroy.";
			}
		}
	}
}
