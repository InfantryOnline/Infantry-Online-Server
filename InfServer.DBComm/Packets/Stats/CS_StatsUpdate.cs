using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
    /// 
    /// </summary>
    public class CS_StatsUpdate<T> : PacketBase
        where T : IClient
    {	// Member Variables
        ///////////////////////////////////////////////////

        public PlayerInstance player;		//The player instance we're referring to
        public Data.PlayerStats stats;
        public ScoreType scoreType;         //Query type
        public Int64 time;
        public DateTime date;

        //Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.StatsUpdate;
		static public event Action<CS_StatsUpdate<T>, T> Handlers;

        public enum ScoreType
        {
            ScoreDaily,
            ScoreWeekly,
            ScoreMonthly,
            ScoreYearly
        }

        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_StatsUpdate()
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
		public CS_StatsUpdate(ushort typeID, byte[] buffer, int index, int count)
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

			stats.Serialize(this);
            Write((byte)scoreType);
            time = date.Ticks;
            Write(time);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			player.id = _contentReader.ReadUInt16();
			player.magic = _contentReader.ReadInt32();

			stats = PlayerStats.Deserialize(_contentReader);
            scoreType = (ScoreType)_contentReader.ReadByte();
            time = _contentReader.ReadInt64();
            //Lets convert our ticks to a date
            date = new DateTime(time);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player stats update";
			}
		}
	}
}
