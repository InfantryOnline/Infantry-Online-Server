using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// SC_Skills updates the player's skill inventory and experience
	/// </summary>
	public class SC_Skills : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int32 experience;
		public Int32 experienceRemaining;						//Unspent experience
		public int skillCount;
		public IEnumerable<Player.SkillItem> skills;			//The player's skill inventory

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Skills;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Skills()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			//TODO: This packet isn't fully reversed yet, but it's a little something like this

			//Write each entry of the skill inventory
			Write(experience);
			Write(experienceRemaining);
			Write((short)2);			//Unknown
			Write((short)skillCount);

			foreach (Player.SkillItem skill in skills)
				Write((ushort)skill.skill.SkillId);

			Skip((Player.SkillItem.MaxSkills - skillCount) * 8);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player skill update.";
			}
		}
	}
}
