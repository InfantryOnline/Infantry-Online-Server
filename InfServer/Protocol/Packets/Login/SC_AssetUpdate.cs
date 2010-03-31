using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_AssetUpdate contains an asset file and details for updating
	/// </summary>
	public class SC_AssetUpdate : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public string filename;
		public int uncompressedLen;

		public byte[] compressedData;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.AssetUpdate;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_AssetUpdate()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			//Contents
			Write((byte)0);
			Write(uncompressedLen);
			Write(uncompressedLen);
			Write(filename, 64);
			Write(compressedData);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Asset update: " + filename;
			}
		}
	}
}
