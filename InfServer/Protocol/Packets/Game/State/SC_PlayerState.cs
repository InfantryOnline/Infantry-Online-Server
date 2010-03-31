using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerState notifies the player of stored information such
	/// as inventory, skills, etc
	/// </summary>
	public class SC_PlayerState : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int32 cash;
		public int inventoryCount;
		public IEnumerable<Player.InventoryItem> inventory;		//The player's inventory

		public Int32 experience;
		public Int32 experienceRemaining;						//Unspent experience
		public int skillCount;
		public IEnumerable<Player.SkillItem> skills;			//The player's skill inventory

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PlayerState;
		

		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerState()
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
			Write((short)0);			//Unknown
			Write((short)inventoryCount);

			foreach (Player.InventoryItem item in inventory)
			{
				Write((ushort)item.item.id);
				Write(item.quantity);
			}

			Skip((Player.InventoryItem.MaxItems - inventoryCount) * 4);

			//Skip the rest
			Skip(88);
			Skip(2);

			//Write each entry of the skill inventory
			Write(experience);	
			Write(experienceRemaining);	
			Write((short)0);			//Unknown
			Write((short)skillCount);

			foreach (Player.SkillItem skill in skills)
				Write((ushort)skill.skill.SkillId);

			Skip((Player.SkillItem.MaxSkills - skillCount) * 8);

			//Skip the rest
			Skip(800);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player state initialize.";
			}
		}
	}
}
