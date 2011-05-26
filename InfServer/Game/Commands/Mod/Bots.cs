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
		static public void spawnBot(Player player, Player recipient, string payload)
		{	//Sanity checks
			if (payload == "" ||
				recipient != null)
			{
				player.sendMessage(-1, "Syntax: *spawnbot [scriptType], [vehicleid]");
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

			Bot newBot = player._arena.newBot(
				typeof(ScriptBot), Convert.ToUInt16(args[1].Trim()), player._state,
				args[0].Trim());
		}

		/// <summary>
		/// Computes a path from the player's location to the given location
		/// </summary>
		static public void computePath(Player player, Player recipient, string payload)
		{	//Sanity checks
			if (payload == "" ||
				recipient != null)
			{
				player.sendMessage(-1, "Syntax: *computepath [exactCoord]");
				return;
			}

			string[] coords = payload.Split(',');
			int x = Convert.ToInt32(coords[0]);
			int y = Convert.ToInt32(coords[1]);

			//Pass it to the pathfinder
			int[] path;
			bool bSuccess = player._arena._pathfinder.calculatePath((short)(player._state.positionX / 16), (short)(player._state.positionY / 16),
																	(short)x, (short)y,
																	out path);

			if (bSuccess)
			{	//Spawn markers on the path!
				ItemInfo item = player._arena._server._assets.getItemByName("Drop Armor");
				LvlInfo level = player._arena._server._assets.Level;

				for (int i = 0; i < path.Length / 5; ++i)
				{
					short cX = (short)(path[i * 5] / level.Width);
					short cY = (short)(path[i * 5] % level.Width);

					cX *= 16;
					cY *= 16;

					player._arena.itemSpawn(item, 1, cX, cY);
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
				"*spawnbot [scriptType], [vehicleid]", 
				InfServer.Data.PlayerPermission.ArenaMod);

			yield return new HandlerDescriptor(computePath, "computepath",
				"Computes a path using pathfinding between your current and the given location.",
				"*computepath [exactCoord]",
				InfServer.Data.PlayerPermission.ArenaMod);
		}
	}
}
