using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Statistics Class
	/// Deals with handling statistics
	///////////////////////////////////////////////////////
	class Logic_Statistics
	{	/// <summary>
		/// Handles all player update packets received from clients
		/// </summary>
		static public void Handle_CS_ChartRequest(CS_ChartRequest pkt, Player player)
		{	//Make sure he has an arena
			if (player._arena == null)
			{
				Log.write(TLog.Error, "Player {0} requested chart with no arena.", player);
				return;
			}

			//What sort of chart request?
			SC_ScoreChart chart = new SC_ScoreChart();

			switch (pkt.type)
			{
				case Helpers.ChartType.ScoreOnlinePlayers:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScoreOnlinePlayers;
						chart.columns = "Online Player Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{	return players[idx];	};
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;
							return players[idx].StatsTotal;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScoreCurrentGame:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScoreCurrentGame;
						chart.columns = "Current Game Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{ return players[idx]; };
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;
							return players[idx].StatsCurrentGame;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScorePreviousGame:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScorePreviousGame;
						chart.columns = "Current Session Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{ return players[idx]; };
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;
							return players[idx].StatsLastGame;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScoreCurrentSession:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScoreCurrentSession;
						chart.columns = "Current Session Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{ return players[idx]; };
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;
							return players[idx].StatsCurrentSession;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScoreLifetime:
					{	//Send the request to the database!
						CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

						req.player = player.toInstance();
						req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreLifetime;
						req.options = pkt.options;

						player._arena._server._db.send(req);
					}
					break;

				default:
					{
						chart.type = Helpers.ChartType.ScoreCurrentGame;
						chart.columns = "Invalid Chart Request";
						chart.dataFunc = delegate(int idx) { return null; };
						player._client.sendReliable(chart, 1);
					}
					break;
			}
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_ChartRequest.Handlers += Handle_CS_ChartRequest;
		}
	}
}
