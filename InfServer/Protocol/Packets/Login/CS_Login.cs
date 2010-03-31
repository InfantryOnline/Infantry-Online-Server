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
