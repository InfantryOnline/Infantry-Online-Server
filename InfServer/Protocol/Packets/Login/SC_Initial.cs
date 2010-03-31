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

		public const ushort TypeID = 2;


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
