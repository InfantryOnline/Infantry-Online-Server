using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerStatsResponse relays a player statistics request to the player
	/// </summary>
	public class SC_PlayerStatsResponse<T> : PacketBase
		where T : IClient
	{	// Member Variables
		///////////////////////////////////////////////////
		public PlayerInstance player;						//The player instance we're referring to

		public CS_PlayerStatsRequest<T>.ChartType type;//The chart type

		public string columns;								//The chart columns
		public byte[] data;									//The stats data

		//Packet routing
		public const ushort TypeID = 3;
		static public event Action<SC_PlayerStatsResponse<T>, T> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerStatsResponse()
			: base(TypeID)
		{}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public SC_PlayerStatsResponse(ushort typeID, byte[] buffer, int index, int count)
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
			Write(columns, 0);

			Write((int)data.Length);
			Write(data);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			player.id = _contentReader.ReadUInt16();
			player.magic = _contentReader.ReadInt32();

			type = (CS_PlayerStatsRequest<T>.ChartType)_contentReader.ReadByte();
			columns = ReadNullString();

			int dataLength = _contentReader.ReadInt32();
			data = _contentReader.ReadBytes(dataLength);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player stats response";
			}
		}
	}
}
