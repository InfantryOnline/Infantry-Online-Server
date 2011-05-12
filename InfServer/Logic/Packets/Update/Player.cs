using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_PlayerUpdate Class
	/// Deals with handling all player-related updates
	///////////////////////////////////////////////////////
	class Logic_PlayerUpdate
	{	/// <summary>
		/// Handles all player update packets received from clients
		/// </summary>
		static public void Handle_CS_PlayerUpdate(CS_PlayerUpdate pkt, Player player)
		{	//Allow the player's arena to handle it
			if (player._arena == null)
			{
				Log.write(TLog.Error, "Player {0} sent update packet with no arena.", player);
				return;
			}

			using (DdMonitor.Lock(player._arena._sync))
				using (LogAssume.Assume(player._arena._logger))
					player._arena.handlePlayerUpdate(player, pkt);
		}

		/// <summary>
		/// Handles the declaration of death from a client
		/// </summary>
		static public void Handle_CS_PlayerDeath(CS_VehicleDeath pkt, Player player)
		{	//Allow the player's arena to handle it
			if (player._arena == null)
			{
				Log.write(TLog.Error, "Player {0} sent death packet with no arena.", player);
				return;
			}

			using (DdMonitor.Lock(player._arena._sync))
				using (LogAssume.Assume(player._arena._logger))
					player._arena.handlePlayerDeath(player, pkt);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_PlayerUpdate.Handlers += Handle_CS_PlayerUpdate;
			CS_VehicleDeath.Handlers += Handle_CS_PlayerDeath;
		}
	}
}
