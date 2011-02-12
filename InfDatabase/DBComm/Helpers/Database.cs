using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{
	/// <summary>
	/// Provides a series of helpful database functions
	/// </summary>
	public partial class DBHelpers
	{	///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Converts binary data to a list of inventory items
		/// </summary>
		static public int binToInventory(List<PlayerStats.InventoryStat> invlist, System.Data.Linq.Binary bin)
		{	//Convert to a byte array
			byte[] invdata = bin.ToArray();

			int count = BitConverter.ToInt16(invdata, 0);
			int idx = 2;

			for (int i = 0; i < count; ++i)
			{
				PlayerStats.InventoryStat stat = new PlayerStats.InventoryStat();

				stat.itemid = BitConverter.ToInt16(invdata, idx);
				stat.quantity = BitConverter.ToInt16(invdata, idx + 2);

				invlist.Add(stat);
				idx += 4;
			}

			return count;
		}

		/// <summary>
		/// Converts a list of inventory items to binary data
		/// </summary>
		static public System.Data.Linq.Binary inventoryToBin(List<PlayerStats.InventoryStat> invlist)
		{	//Create a new byte array
			byte[] invdata = new byte[(invlist.Count * 4) + 2];
			BinaryWriter bw = new BinaryWriter(new MemoryStream(invdata));

			bw.Write((Int16)invlist.Count);

			foreach (PlayerStats.InventoryStat stat in invlist)
			{
				bw.Write((Int16)stat.itemid);
				bw.Write((Int16)stat.quantity);
			}

			return new System.Data.Linq.Binary(invdata);
		}

		/// <summary>
		/// Converts binary data to a list of skills
		/// </summary>
		static public int binToSkills(List<PlayerStats.SkillStat> skilllist, System.Data.Linq.Binary bin)
		{	//Convert to a byte array
			byte[] skilldata = bin.ToArray();

			int count = BitConverter.ToInt16(skilldata, 0);
			int idx = 2;

			for (int i = 0; i < count; ++i)
			{
				PlayerStats.SkillStat stat = new PlayerStats.SkillStat();

				stat.skillid = BitConverter.ToInt16(skilldata, idx);
				stat.quantity = BitConverter.ToInt16(skilldata, idx + 2);

				skilllist.Add(stat);
				idx += 4;
			}

			return count;
		}

		/// <summary>
		/// Converts a list of skill items to binary data
		/// </summary>
		static public System.Data.Linq.Binary skillsToBin(List<PlayerStats.SkillStat> skilllist)
		{	//Create a new byte array
			byte[] skilldata = new byte[(skilllist.Count * 4) + 2];
			BinaryWriter bw = new BinaryWriter(new MemoryStream(skilldata));

			bw.Write((Int16)skilllist.Count);

			foreach (PlayerStats.SkillStat stat in skilllist)
			{
				bw.Write((Int16)stat.skillid);
				bw.Write((Int16)stat.quantity);
			}

			return new System.Data.Linq.Binary(skilldata);
		}
	}
}
