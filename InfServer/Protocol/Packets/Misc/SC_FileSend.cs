using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_FileSend used for testing client functionality
	/// </summary>
	public class SC_FileSend : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public string filePath;
		public string playerFrom;

		public byte[] fileData;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.FileSend;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_FileSend()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);

			Write(filePath, 128);
			Write(playerFrom, 32);

			Write(fileData);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "A file send";
			}
		}
	}
}
