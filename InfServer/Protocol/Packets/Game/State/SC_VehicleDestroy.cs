using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_VehicleDestroy removes a vehicle from the client state 
	/// </summary>
	public class SC_VehicleDestroy : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 vehicleID;		//ID of the vehicle to destroy

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.VehicleDestroy;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_VehicleDestroy()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);
			Write(vehicleID);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Vehicle destruct.";
			}
		}
	}
}
