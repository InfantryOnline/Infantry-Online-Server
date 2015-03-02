using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Bots;
using Assets;

namespace InfServer.Game.Commands.Mod
{
	/// <summary>
	/// Provides a series of functions for handling mod commands
	/// </summary>
	public class Bots
	{
		/// <summary>
		/// Spawns a bot of the specified vehicle id
		/// </summary>
		static public void spawnBot(Player player, Player recipient, string payload, int bong)
		{	//Sanity checks
            if (String.IsNullOrEmpty(payload) ||
				recipient != null)
			{
				player.sendMessage(-1, "Syntax: *spawnbot [scriptType], [vehicleID]");
                player.sendMessage(0, "Optional: *spawnbot [scriptType], [vehicleID], [location]");
				return;
			}

            if (player._server._zoneConfig.bot.maxAmountInArena > 0 
                && player._arena._botsInArena >= player._server._zoneConfig.bot.maxAmountInArena)
            {
                player.sendMessage(-1, "Bot limit reached.");
                return;
            }

			//Spawn a bot near to us
			string[] args = payload.Split(',');
			
			//Does this script type exist?
			if (!Scripting.Scripts.invokerTypeExists(args[0].Trim()))
			{
				player.sendMessage(-1, "Script type doesn't exist.");
				return;
			}

            //Did we type anything?
            if (String.IsNullOrWhiteSpace(args[1]))
            {
                player.sendMessage(-1, "That is not a valid vehicle id.");
                return;
            }

            //Does the vehicle exist?
            try
            {
                if (player._arena._server._assets.getVehicleByID(Convert.ToUInt16(args[1].Trim())) == null)
                {
                    player.sendMessage(-1, "That is not a valid vehicle id.");
                    return;
                }
            }
            catch
            {
                player.sendMessage(-1, "That is not a valid vehicle id.");
                return;
            }

            Protocol.Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
            bool teamBot = args[0].Trim().Contains("Team");

            int x = player._state.positionX;
            int y = player._state.positionY;

            //Are we using a specified location?
            if (args.Count() > 2 && !String.IsNullOrWhiteSpace(args[2]))
            {
                //Is this an exact coord?
                if (args.Count() > 3 && !String.IsNullOrWhiteSpace(args[3]))
                {   //Yes, parse it
                    x = Convert.ToInt32(args[2].Trim()) * 16;
                    y = Convert.ToInt32(args[3].Trim()) * 16;
                }
                else
                {   //No, map point
                    string coord = args[2].Trim().ToLower();
                    if (coord[0] >= 'a' && coord[0] <= 'z' && coord.Length > 1)
                    {
                        x = (((int)coord[0]) - ((int)'a')) * 16 * 80;
                        y = Convert.ToInt32(coord.Substring(1)) * 16 * 80;

                        //We want to spawn in the coord center
                        x += 40 * 16;
                        y -= 40 * 16;
                    }
                }

                newState.positionX = (short)x;
                newState.positionY = (short)y;
                newState.positionZ = 0; //People could spawn them while flying and they would stay in the air
                newState.yaw = player._state.yaw;
                if (teamBot)
                {
                    Bot newBot = player._arena.newBot(typeof(ScriptBot), Convert.ToUInt16(args[1].Trim()),
                        player._team, player, newState, args[0].Trim());
                }
                else
                {
                    Bot newBot = player._arena.newBot(
                        typeof(ScriptBot), Convert.ToUInt16(args[1].Trim()), newState,
                        args[0].Trim());
                }
            }
            else
            {
                newState.positionX = (short)x;
                newState.positionY = (short)y;
                newState.positionZ = 0; //People could spawn them while flying and they would stay in the air
                newState.yaw = player._state.yaw;
                if (teamBot)
                {
                    Bot newBot = player._arena.newBot(typeof(ScriptBot), Convert.ToUInt16(args[1].Trim()),
                        player._team, player, newState, args[0].Trim());
                }
                else
                {
                    Bot newBot = player._arena.newBot(
                        typeof(ScriptBot), Convert.ToUInt16(args[1].Trim()), newState,
                        args[0].Trim());
                }
            }
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(spawnBot, "spawnbot",
				"Spawns a bot using a specified vehicle type and script.",
				"*spawnbot [scriptType], [vehicleID] OR *spawnbot [scriptType], [vehicleID], [location](xx,yy OR A4)", 
				InfServer.Data.PlayerPermission.Mod, true);
		}
	}
}
