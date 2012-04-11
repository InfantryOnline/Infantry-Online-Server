using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// CS_Login contains login credentials from the client
	/// </summary>
	public class CS_Login : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool bCreateAlias;	//Are we attempting to create a new alias?
		public UInt16 Version;		//Version of the connecting client

		public string Username;
		public string SysopPass;
		public string TicketID;

		public UInt32 UID1;			//Unique identifiers
		public UInt32 UID2;
		public UInt32 UID3;			//Portion of the MAC ID?
		public UInt32 NICInfo;		//Some number pertaining to nics

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Login;
		static public event Action<CS_Login, Client<Player>> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_Login(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

        /// <summary>
        /// Generic constructor for creating new empty packets
        /// </summary>
        public CS_Login()
            : base(TypeID)
        {
        }

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, ((Client<Player>)_client));
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Read in junk
			bCreateAlias = _contentReader.ReadBoolean();
			Version = _contentReader.ReadUInt16();
			_contentReader.ReadInt32();

			//Read in strings
			Username = ReadString(32);
			SysopPass = ReadString(32);
			TicketID = ReadString(64);
			
			_contentReader.ReadInt32();

			UID1 = _contentReader.ReadUInt32();
			UID2 = _contentReader.ReadUInt32();
			UID3 = _contentReader.ReadUInt32();
			NICInfo = _contentReader.ReadUInt32();
		}

        /// <summary>
        /// Serializes the data present
        /// </summary>
        public override void Serialize()
        {
            Write((byte)TypeID);
            Write(bCreateAlias);
            Write(Version);

            Write(0);

            Write(Username, 32);
            Write(SysopPass, 32);
            Write(TicketID, 64);
            Write(0);

            Write(UID1);
            Write(UID2);
            Write(UID3);
            Write(NICInfo);
        }

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Login: " + Username;
			}
		}
	}
}
