using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_AssetUpdateInfo contains info on files about to be updated
	/// </summary>
	public class SC_AssetUpdateInfo : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<AssetUpdate> updates;
		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.AssetUpdateInfo;

		public class AssetUpdate
		{
			public string filename;
			public int compressedLength;
		}

		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_AssetUpdateInfo()
			: base(TypeID)
		{
			updates = new List<AssetUpdate>();
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			if (updates.Count == 0)
			{
				Write((byte)TypeID);
				return;
			}

			//Write out each update
			foreach (AssetUpdate update in updates)
			{	//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(update.filename, 28);
				Write(update.compressedLength + 0x4A);		//Header length
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Asset update info";
			}
		}
	}
}
