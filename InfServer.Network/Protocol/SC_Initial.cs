using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Initial is the server's reply to the client initial greeting packet
	/// </summary>
	public class SC_Initial : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int32 connectionID;			//The unique ID for the connection, given by the client
		public Int32 CRCSeed;				//The seed to use for crc

		public byte CRCLen;					//The size of the CRC to use
		public short cryptFlags;			//Flags describing the encryption method to use

		public Int32 serverUDPMax;			//The maximum size of packet we can receive
		public Int32 unk1;

		public const ushort TypeID = (ushort)2;
		static public event Action<SC_Initial, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Initial()
			: base(TypeID)
		{}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public SC_Initial(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, (Client)_client);
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((UInt16)(TypeID << 8));

			//Contents
			Write(Flip(connectionID));
			Write(Flip(CRCSeed));
			Write(CRCLen);
			Write(cryptFlags);
			Write(Flip(serverUDPMax));
			Write(Flip(unk1));
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			connectionID = Flip(_contentReader.ReadInt32());
			CRCSeed = Flip(_contentReader.ReadInt32());
			CRCLen = _contentReader.ReadByte();
			cryptFlags = _contentReader.ReadInt16();
			serverUDPMax = Flip(_contentReader.ReadInt32());
			unk1 = Flip(_contentReader.ReadInt32());
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Server UDP Max: " + serverUDPMax + " / CRCLen: " + CRCLen + " / CryptFlags: " + cryptFlags;
			}
		}
	}
}
