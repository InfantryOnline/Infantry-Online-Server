using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Banner contains banner information for a particular player
	/// </summary>
	public class SC_Banner : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Player player;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.BannerInfo;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Banner()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);
			Write((int)player._id);

			Write(player._bannerData);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player banner data";
			}
		}
	}
}
