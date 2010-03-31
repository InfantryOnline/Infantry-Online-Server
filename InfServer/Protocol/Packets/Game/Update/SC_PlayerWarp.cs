using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerWarp is used to force the client to warp to a location 
	/// </summary>
	public class SC_PlayerWarp : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.WarpMode warpMode;		//Type of warp
		public Int16 invulnTime;	//The amount of time we're invulnerable after spawning
		public Int16 bottomX;		//The warp window
		public Int16 bottomY;		//
		public Int16 topX;			//
		public Int16 topY;			//
		public Int16 energy;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PlayerWarp;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerWarp()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write((byte)warpMode);
			Write(invulnTime);
			Write(topX);
			Write(topY);
			Write(bottomX);
			Write(bottomY);
			Write(energy);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player warp notification";
			}
		}
	}
}
