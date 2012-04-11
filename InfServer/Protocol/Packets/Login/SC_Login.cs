using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Login is used to trigger the rest of the login process for the client
	/// </summary>
	public class SC_Login : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Login_Result result;			//Is our login successful?
		public ushort serverVersion;		//The version of the server

		public string zoneConfig;			//The config file used for this zone
		public string popupMessage;			//Message to present the user with, if any

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Login;
        static public event Action<SC_Login, Client> Handlers;

		public enum Login_Result
		{
			Success = 0,
			CreateAlias = 1,			//Prompt user to create a new alias
			Failed = 2,					//Do not login!
			ZonePassword = 6,			//Ask for a zone password
		}

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, (Client)_client);
        }


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Login()
			: base(TypeID)
		{ }


        public SC_Login(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			//Contents
			Write((byte)result);
			Write(serverVersion);
			Write(zoneConfig, 24);
			Write(popupMessage, 0);
		}

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void  Deserialize()
        {
            result = (Login_Result)_contentReader.ReadByte();
            serverVersion = _contentReader.ReadUInt16();
            zoneConfig = ReadNullString();
            popupMessage = ReadNullString();
        }

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Server Version: " + serverVersion;
			}
		}
	}
}
