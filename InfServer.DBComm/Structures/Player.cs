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

		public int killPoints;
		public int deathPoints;
		public int assistPoints;
		public int bonusPoints;
		public int vehicleKills;
		public int vehicleDeaths;
		public int playSeconds;

		public int zonestat1;
		public int zonestat2;
		public int zonestat3;
		public int zonestat4;
		public int zonestat5;
		public int zonestat6;
		public int zonestat7;
		public int zonestat8;
		public int zonestat9;
		public int zonestat10;
		public int zonestat11;
		public int zonestat12;

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

		public long Points
		{
			get
			{
				return killPoints + assistPoints + bonusPoints;
			}
		}


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Constructor
		/// </summary>
		public PlayerStats()
		{	

		}

		/// <summary>
		/// Constructs the stats object from another stats object
		/// </summary>
		public PlayerStats(PlayerStats stats)
		{
			zonestat1 = stats.zonestat1;
			zonestat2 = stats.zonestat2;
			zonestat3 = stats.zonestat3;
			zonestat4 = stats.zonestat4;
			zonestat5 = stats.zonestat5;
			zonestat6 = stats.zonestat6;
			zonestat7 = stats.zonestat7;
			zonestat8 = stats.zonestat8;
			zonestat9 = stats.zonestat9;
			zonestat10 = stats.zonestat10;
			zonestat11 = stats.zonestat11;
			zonestat12 = stats.zonestat12;

			kills = stats.kills;
			deaths = stats.deaths;
			killPoints = stats.killPoints;
			deathPoints = stats.deathPoints;
			assistPoints = stats.assistPoints;
			bonusPoints = stats.bonusPoints;
			vehicleKills = stats.vehicleKills;
			vehicleDeaths = stats.vehicleDeaths;
			playSeconds = stats.playSeconds;

			cash = stats.cash;
			experience = stats.experience;
			experienceTotal = stats.experienceTotal;
		}

		/// <summary>
		/// Serializes the data stored in the stat class into the packet data
		/// </summary>
		public void Serialize(PacketBase packet)
		{	//Write all our data out
			packet.Write(zonestat1);
			packet.Write(zonestat2);
			packet.Write(zonestat3);
			packet.Write(zonestat4);
			packet.Write(zonestat5);
			packet.Write(zonestat6);
			packet.Write(zonestat7);
			packet.Write(zonestat8);
			packet.Write(zonestat9);
			packet.Write(zonestat10);
			packet.Write(zonestat11);
			packet.Write(zonestat12);

			packet.Write(kills);
			packet.Write(deaths);

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

			stats.zonestat1 = reader.ReadInt32();
			stats.zonestat2 = reader.ReadInt32();
			stats.zonestat3 = reader.ReadInt32();
			stats.zonestat4 = reader.ReadInt32();
			stats.zonestat5 = reader.ReadInt32();
			stats.zonestat6 = reader.ReadInt32();
			stats.zonestat7 = reader.ReadInt32();
			stats.zonestat8 = reader.ReadInt32();
			stats.zonestat9 = reader.ReadInt32();
			stats.zonestat10 = reader.ReadInt32();
			stats.zonestat11 = reader.ReadInt32();
			stats.zonestat12 = reader.ReadInt32();

			stats.kills = reader.ReadInt32();
			stats.deaths = reader.ReadInt32();

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
