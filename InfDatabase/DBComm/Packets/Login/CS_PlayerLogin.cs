using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_PlayerLogin relays a player login request to the database server
	/// </summary>
	public class CS_PlayerLogin<T> : PacketBase
		where T : IClient
	{	// Member Variables
		///////////////////////////////////////////////////
		public PlayerInstance player;		//The player instance we're referring to

		public bool bCreateAlias;			//Is this a create alias attempt?

		public string alias;				//The alias we wish to login as
		public string ticketid;				//The ticketid associated with our account

		public uint UID1;					//The player's unique identifiers
		public uint UID2;					//
		public uint UID3;					//
		public uint UID4;					//

		//Packet routing
		public const ushort TypeID = 2;
		static public event Action<CS_PlayerLogin<T>, T> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public CS_PlayerLogin()
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
		public CS_PlayerLogin(ushort typeID, byte[] buffer, int index, int count)
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

			Write(bCreateAlias);

			Write(alias, 0);
			Write(ticketid, 0);

			Write(UID1);
			Write(UID2);
			Write(UID3);
			Write(UID4);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			player.id = _contentReader.ReadUInt16();
			player.magic = _contentReader.ReadInt32();

			bCreateAlias = _contentReader.ReadBoolean();

			alias = ReadString();
			ticketid = ReadString();

			UID1 = _contentReader.ReadUInt32();
			UID2 = _contentReader.ReadUInt32();
			UID3 = _contentReader.ReadUInt32();
			UID4 = _contentReader.ReadUInt32();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player login request";
			}
		}
	}
}
