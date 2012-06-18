using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Game;
using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ArenaList contains a list of arenas joinable by the player
	/// </summary>
	public class SC_ZoneList : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public IEnumerable<Data.ZoneInstance> zones;
		public Player requestee;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ZoneList;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
        public SC_ZoneList(IEnumerable<Data.ZoneInstance> zoneList, Player forPlayer)
			: base(TypeID)
		{
			zones = zoneList;
			requestee = forPlayer;
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Write out each asset
            foreach (Data.ZoneInstance zone in zones)
			{
				//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(zone._name, 32);
                Write((Int16)zone._playercount);
                Write(zone._ip + "," + zone._port, 32);
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Zone info.";
			}
		}
	}
}
