using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_PlayerStatsRequest contains a statistics request from the game server
	/// </summary>
	public class CS_PlayerStatsRequest<T> : PacketBase
		where T : IClient
	{	// Member Variables
		///////////////////////////////////////////////////
		public PlayerInstance player;		//The player instance we're referring to

		public ChartType type;				//The type of chart requested
		public string options;				//The additional options provided

		public enum ChartType
		{
			ScoreOnlinePlayers = 0,
			ScoreLifetime = 1,
			ScoreCurrentGame = 2,
			ScoreDaily = 3,
			ScoreWeekly = 4,
			ScoreMonthly = 5,
			ScoreYearly = 6,
			ScoreHistoryDaily = 7,
			ScoreHistoryWeekly = 8,
			ScoreHistoryMonthly = 9,
			ScoreHistoryYearly = 10,
			ScorePreviousGame = 17,
			ScoreCurrentSession = 18,
		}

		//Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.PlayerStatsRequest;
		static public event Action<CS_PlayerStatsRequest<T>, T> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public CS_PlayerStatsRequest()
			: base(TypeID)
		{
			player = new PlayerInstance();
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_PlayerStatsRequest(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
			player = new PlayerInstance();
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, (_client as Client<T>)._obj);
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			Write(player.id);
			Write(player.magic);

			Write((byte)type);

			Write(options, 0);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			player.id = _contentReader.ReadUInt16();
			player.magic = _contentReader.ReadInt32();

			type = (ChartType)_contentReader.ReadByte();

			options = ReadNullString();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player stats request";
			}
		}
	}
}
