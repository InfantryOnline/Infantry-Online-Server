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
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(spawnBot, "spawnbot",
				"Spawns a bot using a specified vehicle type and script.",
				"*spawnbot [scriptType], [vehicleid]", 
				InfServer.Data.PlayerPermission.ArenaMod);
		}
	}
}
