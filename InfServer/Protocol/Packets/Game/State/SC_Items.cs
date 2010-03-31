using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Items contains the state of all items in the arena
	/// </summary>
	public class SC_Items : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Arena.ItemDrop singleItem;
		public IEnumerable<Arena.ItemDrop> items;

		public const ushort TypeID = (ushort)(ushort)Helpers.PacketIDs.S2C.Items;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Items()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Optimize for one item
			if (singleItem != null)
			{
				Write((byte)TypeID);

				Write(singleItem.positionX);
				Write(singleItem.positionY);
				Write(singleItem.id);
				Write((ushort)singleItem.item.id);
				Write(singleItem.quantity);
				return;
			}
			
			//Our packet type id!
			Write((byte)TypeID);

			//Write out each asset
			foreach (Arena.ItemDrop item in items)
			{
				Write(item.positionX);
				Write(item.positionY);
				Write(item.id);
				Write((ushort)item.item.id);
				Write(item.quantity);
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Item info.";
			}
		}
	}
}
