using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Assets;

using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
	/// <summary>
	/// Provides a series of functions for handling mod commands
	/// </summary>
	public class Game
	{
		/// <summary>
		/// Restarts the current game
		/// </summary>
		static public void restart(Player player, Player recipient, string payload)
		{	//End the current game
			player._arena.gameEnd();
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(restart, "restart",
				"Restarts the current game.",
				"*restart");
		}
	}
}