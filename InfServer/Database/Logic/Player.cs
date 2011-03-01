using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Connection Class
	/// Deals with player specific database packets
	///////////////////////////////////////////////////////
	class Logic_Player
	{	
		/// <summary>
		/// Handles the servers's player login reply
		/// </summary>
		static public void Handle_SC_PlayerLogin(SC_PlayerLogin<Database> pkt, Database db)
		{	//Attempt to find the player in question
			Player player = db._server.getPlayer(pkt.player);
			if (player == null)
			{
				Log.write(TLog.Warning, "Received login reply for unknown player instance.");
				return;
			}
			
			//Failure?
			if (!pkt.bSuccess)
			{	//Is it a create alias prompt?
				if (pkt.bNewAlias)
					Helpers.Login_Response(player._client, SC_Login.Login_Result.CreateAlias);
				else
					//Notify with the login message if present
					Helpers.Login_Response(player._client, SC_Login.Login_Result.Failed, pkt.loginMessage);
				return;
			}

			//Do we want to load stats?
			if (!pkt.bFirstTimeSetup)
				//Assign the player stats
				player.assignStats(pkt.stats);
			else
				//Flag him as first time
				player._bFirstTimeSetup = true;

			//Let him in!
			Helpers.Login_Response(player._client, SC_Login.Login_Result.Success, pkt.loginMessage);
            player._permissionStatic = pkt.permission;
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			SC_PlayerLogin<Database>.Handlers += Handle_SC_PlayerLogin;
		}
	}
}
