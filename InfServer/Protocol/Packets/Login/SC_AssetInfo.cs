using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_AssetInfo contains info for all the infantry assets, with their 
	/// associated checksums.
	/// </summary>
	public class SC_AssetInfo : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<AssetInfo> assets;
		public bool bOptionalUpdate;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.AssetInfo;
        static public event Action<SC_AssetInfo, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_AssetInfo(List<AssetInfo> assetList)
			: base(TypeID)
		{
			assets = assetList;
		}

        public SC_AssetInfo(ushort typeID, byte[] buffer, int index, int count)
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
		{
			if (assets.Count == 0)
			{
				Write((byte)TypeID);
				return;
			}

			//Write out each asset
			foreach (AssetInfo asset in assets)
			{	//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(Path.GetFileName(asset.filepath), 0);
				Write(asset.checksum);
				Write(bOptionalUpdate);
			}
		}

        public override void Deserialize()
        {
           
        }

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Asset info, " + assets.Count + " files.";
			}
		}
	}
}
