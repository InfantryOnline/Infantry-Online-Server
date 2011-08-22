using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ScoreChart is used for displaying score charts to a player
	/// </summary>
	public class SC_ScoreChart : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.ChartType type;
		public string columns;

		public byte[] data;

		public Func<int, Player> playerFunc;
		public Func<int, Data.PlayerStats> dataFunc;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ShowScoreChart;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ScoreChart()
			: base(TypeID)
		{}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);

			Write((byte)type);
			Write(columns, 128);

			if (data != null)
			{
				Write(data);
				return;
			}

			int idx = 0;
			Data.PlayerStats stats = dataFunc(idx);

			while (stats != null)
			{
				Player p = playerFunc(idx++);

				Write(p._alias, 0);
				Write((p._squad != null ? p._squad : ""), 0);

				Write((short)2);
				Write(stats.vehicleDeaths);
				Write(stats.vehicleKills);
				Write(stats.killPoints);
				Write(stats.deathPoints);
				Write(stats.assistPoints);
				Write(stats.bonusPoints);
				Write(stats.kills);
				Write(stats.deaths);
				Write((int)0);
				Write(stats.playSeconds);
				Write(stats.zonestat1);
				Write(stats.zonestat2);
				Write(stats.zonestat3);
				Write(stats.zonestat4);
				Write(stats.zonestat5);
				Write(stats.zonestat6);
				Write(stats.zonestat7);
				Write(stats.zonestat8);
				Write(stats.zonestat9);
				Write(stats.zonestat10);
				Write(stats.zonestat11);
				Write(stats.zonestat12);

				stats = dataFunc(idx);
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "A score chart update";
			}
		}
	}
}
