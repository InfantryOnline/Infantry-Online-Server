using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_Auth contains login credentials from the zone server
	/// </summary>
	public class CS_Auth<T> : PacketBase
		where T: IClient
	{	// Member Variables
		///////////////////////////////////////////////////
		public int zoneID;			//The ID of the zone we represent
		public string password;		//Our password for entering the zone

		//Packet routing
        public const ushort TypeID = (ushort)DBHelpers.PacketIDs.C2S.Auth;
		static public event Action<CS_Auth<T>, Client<T>> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public CS_Auth()
			: base(TypeID)
		{ }

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_Auth(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, (_client as Client<T>));
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			Write(zoneID);
			Write(password, 0);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			zoneID = _contentReader.ReadInt32();
			password = ReadNullString();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Zone server auth request";
			}
		}
	}
}
