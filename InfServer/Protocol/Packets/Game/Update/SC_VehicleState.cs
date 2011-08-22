using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_VehicleState is used to update an itemdrop in the client 
	/// </summary>
	public class SC_VehicleState : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.ResetFlags flags;	
		public Int16 energy;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.VehicleState;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_VehicleState()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write((byte)flags);
			Write(energy);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Vehicle state update.";
			}
		}
	}
}
