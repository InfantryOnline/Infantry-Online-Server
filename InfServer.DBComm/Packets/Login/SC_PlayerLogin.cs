using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Data;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerLogin relays a player login reply to the zone server
	/// </summary>
	public class SC_PlayerLogin<T> : PacketBase
		where T : IClient
	{	// Member Variables
		///////////////////////////////////////////////////
		public PlayerInstance player;				//The player instance we're referring to
        public string alias;                        //The player alias

		public bool bNewAlias;						//Should we ask the user if he wishes to create a new alias?

		public bool bSuccess;						//Was the login successful?
		public string loginMessage;					//Message to show on login, if any

		public string squad;						//The squad the player is a part of
        public long squadID;

		public Data.PlayerPermission permission;	//The player's permission in this zone
        public bool developer;                      //Are we just a dev?
        public bool admin;                          //Are we an admin?

		public bool bFirstTimeSetup;				//Is it the first time the player is setting up inventory?
		public Data.PlayerStats stats;				//The player's statistics

		public byte[] banner;						//The player's stored banner information

		//Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.S2C.PlayerLogin;
		static public event Action<SC_PlayerLogin<T>, T> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerLogin()
			: base(TypeID)
		{
			stats = new InfServer.Data.PlayerStats();
			player = new PlayerInstance();

            alias = "";
			loginMessage = "";
			squad = "";
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public SC_PlayerLogin(ushort typeID, byte[] buffer, int index, int count)
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
            Write(alias, 0);

			Write(bNewAlias);
			Write(bSuccess);
			Write(loginMessage, 0);

			//If login failed, there's no reason to write further
			if (!bSuccess)
				return;

			Write(squad, 0);
            Write(squadID);
			Write((byte)permission);
            Write(developer);
            Write(admin);

			Write(bFirstTimeSetup);
			
			//If it's a first time, then no need to init rest of the stats
			if (bFirstTimeSetup)
				return;

			stats.Serialize(this);

			if (banner != null && banner.Length == 432)
			{
				Write((bool)true);
				Write(banner);
			}
			else
				Write((bool)false);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			player.id = _contentReader.ReadUInt16();
			player.magic = _contentReader.ReadInt32();
            alias = ReadNullString();

			bNewAlias = _contentReader.ReadBoolean();
			bSuccess = _contentReader.ReadBoolean();
			loginMessage = ReadNullString();

			//If login failed, there's no need to read further
			if (!bSuccess)
				return;

			squad = ReadNullString();
            squadID = _contentReader.ReadInt64();
			permission = (InfServer.Data.PlayerPermission)_contentReader.ReadByte();
            developer = _contentReader.ReadBoolean();
            admin = _contentReader.ReadBoolean();

			bFirstTimeSetup = _contentReader.ReadBoolean();

			//If it's a first time, then no need to init rest of the stats
			if (bFirstTimeSetup)
				return;

			stats = PlayerStats.Deserialize(_contentReader);

			bool bBanner = _contentReader.ReadBoolean();
			if (bBanner)
				banner = _contentReader.ReadBytes(432);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player login reply";
			}
		}
	}
}
