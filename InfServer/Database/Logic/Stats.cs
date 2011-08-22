using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Stats Class
	/// Deals with statistic specific database packets
	///////////////////////////////////////////////////////
	class Logic_Stats
	{
		/// <summary>
		/// Receives a stats response from the server and directs it to the appropriate player
		/// </summary>
		static public void Handle_SC_PlayerStatsResponse(SC_PlayerStatsResponse<Database> pkt, Database db)
		{	//Attempt to find the player in question
			Player player = db._server.getPlayer(pkt.player);
			if (player == null)
			{
				Log.write(TLog.Warning, "Received statistics response for unknown player instance.");
				return;
			}

			//Form and send a response
			SC_ScoreChart scores = new SC_ScoreChart();

			scores.type = (Helpers.ChartType)pkt.type;
			scores.columns = pkt.columns;
			scores.data = pkt.data;

			player._client.sendReliable(scores, 1);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			SC_PlayerStatsResponse<Database>.Handlers += Handle_SC_PlayerStatsResponse;
		}
	}
}
