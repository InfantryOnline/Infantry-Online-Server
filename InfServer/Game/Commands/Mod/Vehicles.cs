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
            if (String.IsNullOrEmpty(payload))
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
            if (String.IsNullOrEmpty(payload) ||
				recipient != null)
			{
				player.sendMessage(-1, "Syntax: *vehicle [vehicleid] or *vehicle [vehicleid],amount");
				return;
			}

            int vehicleid;
            int vehicleamount;
            try
            {
                if (payload.Contains(','))
                {
                    string[] split = payload.Split(',');
                    vehicleid = Convert.ToInt32(split.ElementAt(0));
                    vehicleamount = Convert.ToInt32(split.ElementAt(1).Trim());
                }
                else
                {
                    vehicleid = Convert.ToInt32(payload);
                    vehicleamount = 1;
                }
            }
            catch
            {
                player.sendMessage(-1, "You must use a valid ID number.");
                return;
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
                    newState.fireAngle = player._state.fireAngle;
                    newState.direction = player._state.direction;                   
                    
                    player._arena.newVehicle(
                        vehicle,
                        player._team, null,
                        newState);
                }
            }
		}

        /// <summary>
		/// Finds a vehicle of the specified type
		/// </summary>
        static public void findVehicle(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *findvehicle [vehicleid or vehicle name]");
                return;
            }

            if (Protocol.Helpers.IsNumeric(payload))
            {
                int vehicleid = Convert.ToInt32(payload);

                //Obtain the vehicle indicated
                Assets.VehInfo vehicle = player._server._assets.getVehicleByID(Convert.ToInt32(vehicleid));
                if (vehicle == null)
                {
                    player.sendMessage(-1, "That vehicle doesn't exist.");
                    return;
                }

                player.sendMessage(0, String.Format("[{0}] {1}", vehicle.Id, vehicle.Name));
            }
            else
            {
                List<Assets.VehInfo> vehicles = player._server._assets.getVehicleInfos;
                if (vehicles == null)
                {
                    player.sendMessage(-1, "That vehicle doesn't exist.");
                    return;
                }

                int count = 0;
                payload = payload.ToLower();
                foreach (Assets.VehInfo veh in vehicles)
                {
                    if (veh.Name.ToLower().Contains(payload))
                    {
                        player.sendMessage(0, String.Format("[{0}] {1}", veh.Id, veh.Name));
                        count++;
                    }
                }
                if (count == 0)
                    player.sendMessage(-1, "That vehicle doesn't exist.");
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
				InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(findVehicle, "findvehicle",
                "Finds a vehicle id or name loaded in the zone",
                "*findvehicle [vehicleID or name]",
                InfServer.Data.PlayerPermission.Mod, true);

			yield return new HandlerDescriptor(spawnVehicle, "vehicle",
				"Spawns a new vehicle in your current location",
				"*vehicle [vehicleid] or *vehicle [vehicleid],amount", 
				InfServer.Data.PlayerPermission.Mod, true);
		}
	}
}
