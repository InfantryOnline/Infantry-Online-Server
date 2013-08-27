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
        /// Ends the current game
        /// </summary>
        static public void endgame(Player player, Player recipient, string payload, int bong)
        {   //End the current game
            player._arena.gameEnd();
        }

		/// <summary>
		/// Restarts the current game
		/// </summary>
        static public void restart(Player player, Player recipient, string payload, int bong)
		{	//End the current game and restart a new one
			player._arena.gameEnd();
            player._arena.gameStart();
		}

        /// <summary>
        /// Resets the game state
        /// </summary>
        static public void reset(Player player, Player recipient, string payload, int bong)
        {
            player._arena.gameReset();
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
            yield return new HandlerDescriptor(endgame, "endgame",
                "Ends the current game.",
                "*endgame", InfServer.Data.PlayerPermission.ArenaMod, true);

			yield return new HandlerDescriptor(restart, "restart",
				"Restarts the current game.",
				"*restart", InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(reset, "reset",
                "Resets the current game.",
                "*reset", InfServer.Data.PlayerPermission.ArenaMod, true);
		}
	}
}