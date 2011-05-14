using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Arenas Class
	/// Deals with players entering, leaving and requesting arenas
	///////////////////////////////////////////////////////
	class Logic_Arenas
	{
		/// <summary>
		/// Triggered when the client is attempting to join an arena, complete with security credentials
		/// </summary>
		static public void Handle_CS_ArenaJoin(CS_ArenaJoin pkt, Player player)
		{	//If the player isn't logged in, ignore
			if (!player._bLoggedIn)
			{	//Log and abort
				Log.write(TLog.Warning, "Player {0} tried to send security update while not logged in.", player);
				player.disconnect();
				return;
			}

			//TODO: Check the security update

			//If he's in an arena, get him out of it
			if (player._arena != null)
				player.leftArena();

			//Does he have a specific arena to join?
			Arena match = null;

			if (pkt.ArenaName != "" && pkt.ArenaName != "-2")
				match = player._server.playerJoinArena(player, pkt.ArenaName);

			if (match == null)
				//We need to find our player an arena to inhabit..
				match = player._server.allocatePlayer(player);

			//If we're unable to find an arena, abort
			if (match == null)
			{
				Log.write(TLog.Warning, "Unable to allocate player '{0}' an arena.", player._alias);
				player.disconnect();
				return;
			}

			//Add him to the arena
			match.newPlayer(player);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_ArenaJoin.Handlers += Handle_CS_ArenaJoin;
		}
	}
}
