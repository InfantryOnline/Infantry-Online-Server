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
        static public void defaultVehicle(Player player, Player recipient, string payload, int bong)
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
        static public void spawnVehicle(Player player, Player recipient, string payload, int bong)
		{	//Sanity checks
			if (payload == "" ||
				recipient != null)
			{
				player.sendMessage(-1, "Syntax: *vehicle [vehicleid]");
				return;
			}

            int vehicleid;
            int vehicleamount;
            if (payload.Contains(','))
            {
                vehicleid = Convert.ToInt32(payload.Split(',').ElementAt(0));
                vehicleamount = Convert.ToInt32(payload.Split(',').ElementAt(1));
            }
            else
            {
                vehicleid = Convert.ToInt32(payload);
                vehicleamount = 1;
            }

			//Obtain the vehicle indicated
			Assets.VehInfo vehicle = player._server._assets.getVehicleByID(Convert.ToInt32(vehicleid));

			if (vehicle == null)
			{
				player.sendMessage(-1, "Unable to find specified vehicle.");
				return;
			}

            //You can't spawn dependent vehicles or the Infantry world implodes (literally)
            if (vehicle.Type == Assets.VehInfo.Types.Dependent)
            {
                player.sendMessage(-1, "Can't spawn dependent vehicles.");
                return;
            }

            //Create the vehicles and space them out evenly
            for (int i = 0; i < vehicleamount; i++)
            {
                int radius = i * (int)(vehicle.TriggerRadius * 2.5);
                int numturrets = i * 5 + 1;
                for (int j = 0; j < numturrets; j++)
                {
                    Protocol.Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                    newState.positionX = Convert.ToInt16(player._state.positionX + radius * Math.Cos(Math.PI * 2 * ((double)j / numturrets)));
                    newState.positionY = Convert.ToInt16(player._state.positionY + radius * Math.Sin(Math.PI * 2 * ((double)j / numturrets)));
                    newState.positionZ = player._state.positionZ;
                    newState.yaw = player._state.yaw;
                    player._arena.newVehicle(
                        vehicle,
                        player._team, player,
                        newState);
                }
            }
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
