using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// SC_Inventory updates the player's inventory and cash status
	/// </summary>
	public class SC_Inventory : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int32 cash;
		public int inventoryCount;
		public IEnumerable<Player.InventoryItem> inventory;	//The player's inventory

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Inventory;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Inventory()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			//Write in each entry of the inventory
			Write(cash);
			Write((short)1);			//Unknown
			Write((short)inventoryCount);

			foreach (Player.InventoryItem item in inventory)
			{
				Write((ushort)item.item.id);
				Write(item.quantity);
			}

			Skip((Player.InventoryItem.MaxItems - inventoryCount) * 4);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player inventory update.";
			}
		}
	}
}
