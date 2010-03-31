using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Assets;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_BindVehicle modifies the state of a vehicle
	/// </summary>
	public class SC_BindVehicle : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Vehicle Vehicle1;
		public Vehicle Vehicle2;
		public short vehicleID;
		
		public byte[] extraData;		//Operation specific data

		public const ushort TypeID = (ushort)(ushort)Helpers.PacketIDs.S2C.BindVehicle;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_BindVehicle()
			: base(TypeID)
		{
			vehicleID = -1;
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Typeid
			Write((byte)TypeID);

			Write(Vehicle1._id);
			if (vehicleID != -1)
				Write(vehicleID);
			else
				Write((short)(Vehicle2 == null ? -1 : Vehicle2._id));

			Write((short)((Vehicle1._inhabitant == null) ? 0 : Vehicle1._inhabitant._id));

			switch (Vehicle1._type.Type)
			{
				case VehInfo.Types.Car:
					Write(Vehicle1._state.health);
					Write(Vehicle1._state.positionX);
					Write(Vehicle1._state.positionY);
					Write(Vehicle1._state.positionZ);
					Write(Vehicle1._state.yaw);
					Write((byte)0);
					break;

				case VehInfo.Types.Dependent:
					Write((short)((Vehicle1._parent == null) ? -1 : Vehicle1._parent._id));
					Write((short)Vehicle1._state.health);
					Write(Vehicle1._state.positionX);
					Write(Vehicle1._state.positionY);
					Write(Vehicle1._state.positionZ);
					Write(Vehicle1._state.yaw);
					Write((byte)Vehicle1._parentSlot);
					Write(Vehicle1._state.yaw);
					Write(Vehicle1._state.yaw);
					break;

				case VehInfo.Types.Spectator:
					Write(Vehicle1._state.positionX);
					Write(Vehicle1._state.positionY);
					break;

				case VehInfo.Types.Computer:
					Write(Vehicle1._state.health);
					Write((short)0);
					Write(Vehicle1._state.positionX);
					Write(Vehicle1._state.positionY);
					Write(Vehicle1._state.positionZ);
					Write(Vehicle1._state.yaw);
					Write((byte)0);
					Write((Int32)1);
					Write((Int32)Vehicle1._state.yaw);
					break;
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Vehicle update";
			}
		}
	}
}
