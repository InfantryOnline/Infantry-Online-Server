using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Projectile notifies the client of a new projectile
	/// </summary>
	public class SC_Projectile : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int16 projectileID;
		public Int16 playerID;

		public Int16 posX;
		public Int16 posY;
		public Int16 posZ;
		public byte yaw;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Projectile;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Projectile()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(yaw);
			Write(posX);
			Write(posY);
			Write(posZ);
			Write(playerID);
			Write(projectileID);
			Write((short)1);		//Unknown
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Projectile notification";
			}
		}
	}
}
