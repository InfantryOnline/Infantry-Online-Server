using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Players contains updates regarding players in the same arena 
	/// </summary>
	public class SC_Players : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<PlayerInfo> players;

		public const ushort TypeID = 3;

		public class PlayerInfo
		{	//Player credentials
			public string team;
			public string name;
			public string squad;

			public UInt16 playerID;			//Our playerID
			public UInt16 vehicleTypeID;	//The type id of our vehicle
			public UInt16 unk3;
			public UInt32 unk4;
			public bool unk5;
		}


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Players()
			: base(TypeID)
		{
			players = new List<PlayerInfo>();
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			if (players.Count == 0)
			{
				Write((byte)TypeID);
				return;
			}

			//Write out each asset
			foreach (PlayerInfo player in players)
			{	//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(player.team, 32);
				Write(player.name, 32);
				Write(player.squad, 32);

				Write(player.playerID);
				Write(player.vehicleTypeID);
				Write(player.unk3);
				Write(player.unk4);
				Write(player.unk5);
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player info, " + players.Count + " players.";
			}
		}
	}
}
