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
	public class CS_SquadMatch<T> : PacketBase
		where T : IClient
	{	// Member Variables
		///////////////////////////////////////////////////

        public long winner;
        public long loser;
        public SquadStats wStats;
        public SquadStats lStats;

        public class SquadStats
        {
            public int kills;
            public int deaths;
            public int points;
        }
        

		//Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.SquadStats;
		static public event Action<CS_SquadMatch<T>, T> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public CS_SquadMatch()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
        public CS_SquadMatch(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{

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

            Write(winner);
            Write(loser);

            Write(wStats.kills);
            Write(wStats.deaths);
            Write(wStats.points);


            Write(lStats.kills);
            Write(lStats.deaths);
            Write(lStats.points);

		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
            winner = _contentReader.ReadInt64();
            loser = _contentReader.ReadInt64();

            wStats = new SquadStats();
            wStats.kills = _contentReader.ReadInt32();
            wStats.deaths = _contentReader.ReadInt32();
            wStats.points = _contentReader.ReadInt32();

            lStats = new SquadStats();
            lStats.kills = _contentReader.ReadInt32();
            lStats.deaths = _contentReader.ReadInt32();
            lStats.points = _contentReader.ReadInt32();
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
