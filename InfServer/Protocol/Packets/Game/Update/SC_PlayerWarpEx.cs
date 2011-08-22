using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerWarpEx is used to force the client to warp to a location 
	/// </summary>
	public class SC_PlayerWarpEx : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.ResetFlags warpFlags;		//Type of warp
		public Helpers.ObjectState state;

		public Int16 invulnTimer;
		public Int16 warpRadius;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PlayerWarpEx;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerWarpEx()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write((byte)warpFlags);	
			Write(invulnTimer);
			Write(state.positionX);
			Write(state.positionY);
			Write(warpRadius);
			Write((byte)0);
			Write(state.energy);
			Write((short)state.yaw);
			Write(state.velocityX);	
			Write(state.velocityY);	
			Write(state.velocityZ);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player warp ex notification";
			}
		}
	}
}
