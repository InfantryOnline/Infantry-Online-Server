using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	// Member Classes
		//////////////////////////////////////////////////


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Notifies a player of a vehicle death
		/// </summary>
		static public void Vehicle_RouteDeath(
			Player p, Player killer, Vehicle dead, Player occupier)
		{	//Prepare a death packet
			SC_VehicleDeath not = new SC_VehicleDeath();

			not.type = KillType.Player;

			not.playerID = (occupier == null) ? (ushort) 0 : occupier._id;
			not.vehicleID = dead._id;
			not.killerID = (killer == null) ? 0 : (uint)killer._id;
			not.positionX = dead._state.positionX;
			not.positionY = dead._state.positionY;
			not.yaw = dead._state.yaw;

			//Send it to each player
			p._client.sendReliable(not);
		}

		/// <summary>
		/// Notifies a group of players of a vehicle death
		/// </summary>
		static public void Vehicle_RouteDeath(
			IEnumerable<Player> p, Player killer, Vehicle dead, Player occupier)
		{	//Prepare a death packet
			SC_VehicleDeath not = new SC_VehicleDeath();

			not.type = KillType.Player;

			not.playerID = (occupier == null) ? (ushort) 0 : occupier._id;
			not.vehicleID = dead._id;
			not.killerID = (killer == null) ? 0 : (uint)killer._id;
			not.positionX = dead._state.positionX;
			not.positionY = dead._state.positionY;
			not.yaw = dead._state.yaw;

			//Send it to each player
			foreach (Player player in p)
				player._client.sendReliable(not);
		}

		/// <summary>
		/// Sets the vehicle's energy
		/// </summary>
		static public void Vehicle_SetEnergy(Player p, short energy)
		{
			SC_VehicleState state = new SC_VehicleState();

			state.energy = energy;

			p._client.sendReliable(state);
		}

		/// <summary>
		/// Resets aspects of a vehicle's state
		/// </summary>
		static public void Vehicle_ResetState(Player p, bool resetEnergy, bool resetHealth, bool resetVelocity)
		{
			SC_VehicleState state = new SC_VehicleState();

			if (resetEnergy)
				state.flags |= Helpers.ResetFlags.ResetEnergy;
			if (resetHealth)
				state.flags |= Helpers.ResetFlags.ResetHealth;
			if (resetVelocity)
				state.flags |= Helpers.ResetFlags.ResetVelocity;

			p._client.sendReliable(state);
		}
	}
}
