using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Assets;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Vehicles contains updates regarding vehicles in the arena 
	/// </summary>
	public class SC_Vehicles : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public IEnumerable<Vehicle> vehicles;
		public Vehicle singleUpdate;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Vehicles;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Vehicles()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Serializes a single vehicle's data to the packet
		/// </summary>
		private void serializeVehicle(Vehicle veh)
		{
			Write((byte)TypeID);

			Write(veh._id);
			Write((ushort)veh._type.Id);

			if (veh is InfServer.Bots.Bot)
				Write(veh._id);
			else
				Write((short)((veh._inhabitant == null) ? -1 : veh._inhabitant._id));

			switch (veh._type.Type)
			{
				case VehInfo.Types.Car:
					Write((byte)10);		//Sizeof

					Write(veh._state.health);
					Write(veh._state.positionX);
					Write(veh._state.positionY);
					Write(veh._state.positionZ);
					Write(veh._state.yaw);
					Write((byte)0);
					break;

				case VehInfo.Types.Dependent:
					Write((byte)14);		//Sizeof

					Write((short)((veh._parent == null) ? -1 : veh._parent._id));
					Write((short)veh._state.health);
					Write(veh._state.positionX);
					Write(veh._state.positionY);
					Write(veh._state.positionZ);
					Write(veh._state.yaw);
					Write((byte)veh._parentSlot);
					Write(veh._state.yaw);
					Write(veh._state.yaw);
					break;

				case VehInfo.Types.Spectator:
					Write((byte)4);		//Sizeof

					Write(veh._state.positionX);
					Write(veh._state.positionY);
					break;

				case VehInfo.Types.Computer:
					Write((byte)20);		//Sizeof

					Write(veh._state.health);
					Write((short)veh._team._id);
					Write(veh._state.positionX);
					Write(veh._state.positionY);
					Write(veh._state.positionZ);
					Write(veh._state.yaw);
					Write((byte)0);
					Write((Int32)1);
					Write((Int32)veh._state.yaw);
					break;
			}
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Only one vehicle?
			if (singleUpdate != null)
			{
				serializeVehicle(singleUpdate);
				return;
			}
			
			//Write out each asset
			foreach (Vehicle vehicle in vehicles)
				//Serialize!
				serializeVehicle(vehicle);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Vehicle info.";
			}
		}
	}
}
