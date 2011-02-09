using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Network;

namespace InfServer.Data
{	/// <summary>
	/// Used to distinguish player instances between each other
	/// </summary>
	public class PlayerInstance
	{
		public ushort id;
		public int magic;
	}
	
	/// <summary>
	/// The permission level of the player in a zone or global
	/// </summary>
	public enum PlayerPermission
	{
		Normal = 0,
		ArenaMod = 1,
		Mod = 2,
		SMod = 3,
		Sysop = 4,
	}

	/// <summary>
	/// Contains player statistics
	/// </summary>
	public class PlayerStats
	{	// Member Variables
		///////////////////////////////////////////////////
		public int kills;
		public int deaths;

		public int points;
		public int killPoints;
		public int deathPoints;
		public int assistPoints;
		public int bonusPoints;
		public int vehicleKills;
		public int vehicleDeaths;
		public int playSeconds;

		public int altstat1;
		public int altstat2;
		public int altstat3;
		public int altstat4;
		public int altstat5;
		public int altstat6;
		public int altstat7;
		public int altstat8;

		public int cash;
		public List<InventoryStat> inventory;

		public struct InventoryStat
		{
			public int itemid;
			public int quantity;
		}

		public int experience;
		public int experienceTotal;
		public List<SkillStat> skills;

		public struct SkillStat
		{
			public int skillid;
			public int quantity;
		}

		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Serializes the data stored in the stat class into the packet data
		/// </summary>
		public void Serialize(PacketBase packet)
		{	//Write all our data out
			packet.Write(altstat1);
			packet.Write(altstat2);
			packet.Write(altstat3);
			packet.Write(altstat4);
			packet.Write(altstat5);
			packet.Write(altstat6);
			packet.Write(altstat7);
			packet.Write(altstat8);

			packet.Write(kills);
			packet.Write(deaths);

			packet.Write(points);
			packet.Write(killPoints);
			packet.Write(deathPoints);
			packet.Write(assistPoints);
			packet.Write(bonusPoints);
			packet.Write(vehicleKills);
			packet.Write(vehicleDeaths);
			packet.Write(playSeconds);

			packet.Write(cash);

			packet.Write((Int16)inventory.Count);
			foreach (PlayerStats.InventoryStat istat in inventory)
			{
				packet.Write(istat.itemid);
				packet.Write(istat.quantity);
			}

			packet.Write(experience);
			packet.Write(experienceTotal);

			packet.Write((Int16)skills.Count);
			foreach (PlayerStats.SkillStat sstat in skills)
			{
				packet.Write(sstat.skillid);
				packet.Write(sstat.quantity);
			}
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into a playerstats class.
		/// </summary>
		static public PlayerStats Deserialize(BinaryReader reader)
		{	//Ready our object
			PlayerStats stats = new PlayerStats();

			stats.altstat1 = reader.ReadInt32();
			stats.altstat2 = reader.ReadInt32();
			stats.altstat3 = reader.ReadInt32();
			stats.altstat4 = reader.ReadInt32();
			stats.altstat5 = reader.ReadInt32();
			stats.altstat6 = reader.ReadInt32();
			stats.altstat7 = reader.ReadInt32();
			stats.altstat8 = reader.ReadInt32();

			stats.kills = reader.ReadInt32();
			stats.deaths = reader.ReadInt32();

			stats.points = reader.ReadInt32();
			stats.killPoints = reader.ReadInt32();
			stats.deathPoints = reader.ReadInt32();
			stats.assistPoints = reader.ReadInt32();
			stats.bonusPoints = reader.ReadInt32();
			stats.vehicleKills = reader.ReadInt32();
			stats.vehicleDeaths = reader.ReadInt32();
			stats.playSeconds = reader.ReadInt32();

			stats.cash = reader.ReadInt32();

			int i = reader.ReadInt16();
			stats.inventory = new List<PlayerStats.InventoryStat>(i);

			for (; i > 0; --i)
			{
				PlayerStats.InventoryStat ist = new PlayerStats.InventoryStat();

				ist.itemid = reader.ReadInt32();
				ist.quantity = reader.ReadInt32();

				stats.inventory.Add(ist);
			}

			stats.experience = reader.ReadInt32();
			stats.experienceTotal = reader.ReadInt32();

			i = reader.ReadInt16();
			stats.skills = new List<PlayerStats.SkillStat>(i);

			for (; i > 0; --i)
			{
				PlayerStats.SkillStat sst = new PlayerStats.SkillStat();

				sst.skillid = reader.ReadInt32();
				sst.quantity = reader.ReadInt32();

				stats.skills.Add(sst);
			}

			//All done!
			return stats;
		}
	}
}
