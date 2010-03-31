using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Bots;

namespace InfServer.Game.Commands.Mod
{
	/// <summary>
	/// Provides a series of functions for handling mod commands
	/// </summary>
	public class Vehicles
	{
		/// <summary>
		/// Sets the player's default vehicle to the one specified
		/// </summary>
		static public void defaultVehicle(Player player, Player recipient, string payload)
		{	//Sanity checks
			if (payload == "")
			{
				player.sendMessage(-1, "Syntax: *defaultvehicle [vehicleid] or ::*defaultvehicle [vehicleid]");
				return;
			}

			//Set the vehicle
			Player target = (recipient == null) ? player : recipient;
			Assets.VehInfo vehicle = target._server._assets.getVehicleByID(Convert.ToInt32(payload));

			if (vehicle == null)
			{
				player.sendMessage(-1, "Unable to find specified vehicle.");
				return;
			}

			target.setDefaultVehicle(vehicle);
		}

		/// <summary>
		/// Spawns a vehicle of the specified type
		/// </summary>
		static public void spawnVehicle(Player player, Player recipient, string payload)
		{	//Sanity checks
			if (payload == "" ||
				recipient != null)
			{
				player.sendMessage(-1, "Syntax: *vehicle [vehicleid]");
				return;
			}

			//Obtain the vehicle indicated
			Assets.VehInfo vehicle = player._server._assets.getVehicleByID(Convert.ToInt32(payload));

			if (vehicle == null)
			{
				player.sendMessage(-1, "Unable to find specified vehicle.");
				return;
			}

			//Attempt to create it
			player._arena.newVehicle(
				vehicle,
				player._team, player,
				player._state);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(defaultVehicle, "defaultvehicle",
				"Changes the player's default vehicle.",
				"*defaultvehicle [vehicleid] or ::*defaultvehicle [vehicleid]", 
				InfServer.Data.PlayerPermission.ArenaMod);

			yield return new HandlerDescriptor(spawnVehicle, "vehicle",
				"Spawns a new vehicle in your current location",
				"*vehicle [vehicleid]", 
				InfServer.Data.PlayerPermission.ArenaMod);
		}
	}
}
